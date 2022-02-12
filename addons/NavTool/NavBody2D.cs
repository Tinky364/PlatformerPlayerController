using Godot;
using Godot.Collections;

namespace NavTool
{
    [Tool]
    public class NavBody2D : KinematicBody2D
    {
        public NavArea2D NavArea { get; private set; }
        public NavChar2D NavChar { get; private set; }
        public Tween Tween { get; private set; }
        private CollisionShape2D _shape;
        private Area2D _area;
        
        [Export]
        public bool DebugEnabled { get; private set; }
        [Export]
        private bool _isOnBodyCollidingActive;
        [Export]
        private NodePath _navAreaPath = default;
        [Export]
        private NodePath _targetNavBodyPath = default;
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity = 600f;
        [Export(PropertyHint.Range, "0.1,20,0.05,or_greater")] 
        protected float GroundRayLength = 0.1f;
        [Export(PropertyHint.Range, "0.1,200,or_greater")] 
        private float _navPosRayLength = 35f;
        
        public NavBody2D TargetNavBody { get; private set; }
        protected PhysicsBody2D CollidingBody;
        protected Physics2DDirectSpaceState SpaceState;
        protected Dictionary GroundRay;
        private Dictionary _navPosRay;
        public Vector2 NavPos { get; private set; }
        public Vector2 ShapeExtents { get; private set; }
        public Vector2 ShapeSizes => ShapeExtents * 2f;
        public Vector2 Velocity;
        protected Vector2 GroundHitPos { get; private set; }
        protected uint GroundCollisionMask = 6; // 2^(2-1) + 2^(3-1) -> Layer 2,3
        public int Direction { get; set; } = 1;
        private float _lerpPosX;
        public bool IsInactive
        {
            get => _isInactive;
            set
            {
                _isInactive = value;
                SetProcess(!value);
                SetPhysicsProcess(!value);
                foreach (Node child in GetChildren())
                {
                    child.SetProcess(value);
                    child.SetPhysicsProcess(value);
                }
            }
        }
        protected bool IsUnhurtable { get; set; }
        protected bool IsOnGround { get; set; }
        protected bool IsLerping { get; private set; }
        protected bool HasGroundRayDisabled;
        private bool _isColliding;
        private bool _isInactive;

        public override void _EnterTree()
        {
            if (Engine.EditorHint) return;
            Tween = new Tween();
            AddChild(Tween);
            Tween.Name = "Tween";
            Tween.PlaybackProcessMode = Tween.TweenProcessMode.Physics;
        }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            NavChar = GetNode<NavChar2D>("NavChar2D");
            _area = GetNodeOrNull<Area2D>("Area2D");
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            SpaceState = GetWorld2d().DirectSpaceState;
            if (_navAreaPath != null) 
                NavArea = GetNodeOrNull<NavArea2D>(_navAreaPath);
            if (_targetNavBodyPath != null) 
                TargetNavBody = GetNodeOrNull<NavBody2D>(_targetNavBodyPath);
            if (_shape.Shape is RectangleShape2D shape) 
                ShapeExtents = shape.Extents;
            if (NavChar.Shape.Shape is RectangleShape2D navCharShape)
                navCharShape.Extents = new Vector2(ShapeExtents.x, 1f);
            if (NavArea != null && !NavArea.IsPositionInArea(GlobalPosition))
                GlobalPosition = NavArea.GlobalPosition;
            _area?.Connect("body_entered", this, nameof(OnBodyEntered));
            _area?.Connect("body_exited", this, nameof(OnBodyExited));
            NavArea?.Connect("ScreenEntered", this, nameof(OnScreenEnter));
            NavArea?.Connect("ScreenExited", this, nameof(OnScreenExit));
            Tween.Connect("tween_started", this, nameof(OnMoveLerpStarted));
            Tween.Connect("tween_completed", this, nameof(OnMoveLerpCompleted));
            SetNavPos();
        }

        private void OnMoveLerpStarted(Object obj, NodePath key) => IsLerping = true;
        
        private void OnMoveLerpCompleted(Object obj, NodePath key)
        {
            Tween.RemoveAll();
            Velocity = Vector2.Zero;
            IsLerping = false;
        }
        
        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;
            CastGroundRay();
            SetNavPos();
            SetLerpPosWhileIsNotLerping();
            OnBodyColliding(CollidingBody);
        }

        private void SetLerpPosWhileIsNotLerping()
        {
            if (!IsLerping) _lerpPosX = GlobalPosition.x;
        }
        
        public Vector2 DirectionTo(Vector2 to) => NavPos.DirectionTo(to);

        public float DistanceTo(Vector2 to) => NavPos.DistanceTo(to);

        public void MoveLerp(float targetPosX, float duration, Tween.TransitionType transitionType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.InOut, float delay = 0f)
        {
            if (NavArea != null)
            {
                if (targetPosX < NavArea.ReachableAreaRect.Position.x)
                    targetPosX = NavArea.ReachableAreaRect.Position.x;
                else if (targetPosX > NavArea.ReachableAreaRect.End.x)
                    targetPosX = NavArea.ReachableAreaRect.End.x;
            }
            Tween.InterpolateProperty(this, "_lerpPosX", null, targetPosX, duration, transitionType, easeType, delay);
            Tween.Start();
        }
        
        public void MoveLerpWithSpeed(float targetPosX, float speed, Tween.TransitionType transitionType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.InOut, float delay = 0f)
        {
            float duration = Mathf.Abs(targetPosX - NavPos.x) / speed;
            MoveLerp(targetPosX, duration, transitionType, easeType, delay);
        }

        protected float CalculateLerpMotion(float delta)
        {
            return (_lerpPosX - GlobalPosition.x) / delta;
        } 

        public void StopLerp()
        {
            Tween.Stop(this, "_lerpPosX");
            OnMoveLerpCompleted(null, null);
        } 

        protected Vector2 MoveAndSlideInArea(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (NavArea != null)
            {
                if (NavPos.x + velocity.x * delta < NavArea.AreaRect.Position.x && velocity.x < 0
                    || NavPos.x + velocity.x * delta > NavArea.AreaRect.End.x && velocity.x > 0)
                {
                    velocity.x = 0;
                }
            }
            return MoveAndSlide(velocity, upDirection);
        }

        private void SetNavPos()
        {
            Vector2 rightHitPos;
            // Cast left ray.
            _navPosRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ShapeExtents.x, 0f),
                GlobalPosition + new Vector2(-ShapeExtents.x, _navPosRayLength),
                new Array {this},
                GroundCollisionMask
            );
            if (_navPosRay.Count > 0) // When left ray hits.
            {
                Vector2 leftHitPos = (Vector2) _navPosRay["position"]; // Left ray hit position.

                // Cast right ray.
                _navPosRay = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    GlobalPosition + new Vector2(ShapeExtents.x, _navPosRayLength),
                    new Array {this},
                    GroundCollisionMask
                );
                if (_navPosRay.Count > 0) // When both rays hit.
                {
                    rightHitPos = (Vector2) _navPosRay["position"]; // Right ray hit position.

                    // Decides position according to the close hit position.
                    if (rightHitPos.DistanceTo(GlobalPosition) >
                        leftHitPos.DistanceTo(GlobalPosition))
                        NavPos = leftHitPos + new Vector2(ShapeExtents.x, 0);
                    else
                        NavPos = rightHitPos + new Vector2(-ShapeExtents.x, 0);
                }
                else // When only left ray hits.
                    NavPos = leftHitPos + new Vector2(ShapeExtents.x, 0);
            }
            else // When left ray does not hit.
            {
                // Cast right ray.
                _navPosRay = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    GlobalPosition + new Vector2(ShapeExtents.x, _navPosRayLength),
                    new Array {this},
                    GroundCollisionMask
                );
                if (_navPosRay.Count > 0) // When only right ray hits. 
                {
                    rightHitPos = (Vector2) _navPosRay["position"]; // Right ray hit position.

                    NavPos = rightHitPos + new Vector2(-ShapeExtents.x, 0);
                }
                else // When any rays do not hit.
                {
                    NavPos = GlobalPosition;
                }
            }
        }
        
        private void OnBodyEntered(Node node)
        {
            if (!(node is PhysicsBody2D body)) return;
            _isColliding = true;
            CollidingBody = body;
        }
        
        private void OnBodyColliding(Node body)
        {
            if (!_isOnBodyCollidingActive) return;
            if (!_isColliding) return;
            
            if (!(body is NavBody2D targetNavBody)) return;
            if (targetNavBody.IsUnhurtable) return;
            Events.Singleton.EmitSignal(
                "Damaged",
                targetNavBody,
                1,
                this,
                DirectionTo(targetNavBody.NavPos)
            );
        }
        
        private void OnBodyExited(Node node)
        {
            if (!(node is PhysicsBody2D body)) return;
            if (body != CollidingBody) return;
            _isColliding = false;
            CollidingBody = null;
        }
        
        private void CastGroundRay()
        {
            if (HasGroundRayDisabled) return;

            // Raycast from the left bottom corner.
            GroundRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ShapeExtents.x, -1f), 
                GlobalPosition + new Vector2(-ShapeExtents.x, GroundRayLength), 
                new Array {this},
                GroundCollisionMask
            );
            if (GroundRay.Count > 0)
            {
                GroundHitPos = (Vector2) GroundRay["position"] + new Vector2(ShapeExtents.x, 0);
                return;
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner.
            GroundRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ShapeExtents.x, -1f),
                GlobalPosition + new Vector2(ShapeExtents.x, GroundRayLength),
                new Array {this},
                GroundCollisionMask
            );
            if (GroundRay.Count > 0)
            {
                GroundHitPos = (Vector2) GroundRay["position"] + new Vector2(-ShapeExtents.x, 0);
            }
        }

        protected virtual void OnScreenEnter() => IsInactive = false;

        protected virtual void OnScreenExit() => IsInactive = true;

        protected bool IsGroundAngleEnough(float groundAngle, float limit)
            => groundAngle > -limit && groundAngle < limit;
    
        protected float CalculateGroundAngle(Vector2 normal) 
            => Mathf.Rad2Deg(normal.AngleTo(Vector2.Up));
        
        public override string _GetConfigurationWarning()
        {
            if (!Engine.EditorHint) return "";
            for (int i = 0; i < GetChildCount(); i++)
            {
                if (GetChild(i) is NavChar2D)
                    return "";
            }
            return "This node has no NavChar2D. Consider adding a NavChar2D as a child.";
        }
    }
}
