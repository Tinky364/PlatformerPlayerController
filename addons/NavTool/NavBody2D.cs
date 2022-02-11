using Godot;
using Godot.Collections;

namespace NavTool
{
    [Tool]
    public class NavBody2D : KinematicBody2D
    {
        protected CollisionShape2D Shape;
        public NavArea2D NavArea { get; private set; }
        public NavChar2D NavChar { get; private set; }
        public Tween Tween { get; }
        private Area2D _area;
        
        [Export]
        private NodePath _navAreaPath = default;
        [Export]
        private NodePath _targetNavBodyPath = default;
        [Export(PropertyHint.Range, "0.1,200,or_greater")] 
        private float _groundRayLengthForChar = 35f;
        [Export]
        private bool _whileCollisionActive;
        
        protected Physics2DDirectSpaceState SpaceState;
        public NavBody2D TargetNavBody { get; private set; }
        private KinematicBody2D _collidingBody;
        private Dictionary _ray;
        public Vector2 NavPos { get; private set; }
        public Vector2 ShapeExtents { get; private set; }
        public Vector2 ShapeSizes => ShapeExtents * 2f;
        protected bool IsUnhurtable { get; set; }
        private bool _isInactive;
        public bool IsInactive
        {
            get => _isInactive;
            set
            {
                _isInactive = value;
                SetProcess(!value);
                SetPhysicsProcess(!value);
            }
        }
        private bool _isColliding;
        public bool IsOnGround { get; set; }
        protected uint GroundCollisionMask = 6; // 2^(2-1) + 2^(3-1) -> Layer 2,3
            
        protected NavBody2D()
        {
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
            Shape = GetNode<CollisionShape2D>("CollisionShape2D");
            if (_navAreaPath != null) 
                NavArea = GetNodeOrNull<NavArea2D>(_navAreaPath);
            if (_targetNavBodyPath != null) 
                TargetNavBody = GetNodeOrNull<NavBody2D>(_targetNavBodyPath);
            SpaceState = GetWorld2d().DirectSpaceState;
            if (Shape.Shape is RectangleShape2D shape) ShapeExtents = shape.Extents;
            if (NavChar.Shape.Shape is RectangleShape2D navCharShape)
                navCharShape.Extents = new Vector2(ShapeExtents.x, 1f);

            if (NavArea != null && !NavArea.IsPositionInArea(GlobalPosition))
                SetBodyPosition(NavArea.GlobalPosition);
            
            _area?.Connect("body_entered", this, nameof(OnBodyEntered));
            _area?.Connect("body_exited", this, nameof(OnBodyExited));

            FindGroundPosition();
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;
            
            WhileColliding(_collidingBody);
            FindGroundPosition();
        }

        private void WhileColliding(Node body)
        {
            if (!_whileCollisionActive) return;
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
        
        public Vector2 DirectionTo(Vector2 to) => NavPos.DirectionTo(to);

        public float DistanceTo(Vector2 to) => NavPos.DistanceTo(to);

        public void LerpWithDuration(float targetPosX, float duration,
                                     Tween.TransitionType transitionType = Tween.TransitionType.Linear,
                                     Tween.EaseType easeType = Tween.EaseType.InOut,
                                     float delay = 0f)
        {
            if (targetPosX < NavArea?.ReachableAreaRect.Position.x)
                targetPosX = NavArea.ReachableAreaRect.Position.x;
            else if (targetPosX > NavArea?.ReachableAreaRect.End.x)
                targetPosX = NavArea.ReachableAreaRect.End.x;

            Tween.InterpolateProperty(
                this,
                "global_position:x",
                null,
                targetPosX,
                duration,
                transitionType,
                easeType,
                delay
            );
            Tween.Start();
        }
        
        public void LerpWithSpeed(float targetPosX, float speed,
                                  Tween.TransitionType transitionType = Tween.TransitionType.Linear,
                                  Tween.EaseType easeType = Tween.EaseType.InOut,
                                  float delay = 0f)
        {
            if (targetPosX < NavArea?.ReachableAreaRect.Position.x)
                targetPosX = NavArea.ReachableAreaRect.Position.x;
            else if (targetPosX > NavArea?.ReachableAreaRect.End.x)
                targetPosX = NavArea.ReachableAreaRect.End.x;

            Tween.InterpolateProperty(
                this,
                "global_position:x",
                null,
                targetPosX,
                Mathf.Abs(targetPosX - NavPos.x) / speed,
                transitionType,
                easeType,
                delay
            );
            Tween.Start();
        }

        public void StopLerp()
        {
            Tween.Stop(this, "global_position:x");
        }

        private void SetBodyPosition(Vector2 position)
        {
            GlobalPosition = position;
        }

        public Vector2 MoveAndSlideInArea(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (NavPos.x + velocity.x * delta < NavArea?.AreaRect.Position.x &&
                velocity.x < 0
                || NavPos.x + velocity.x * delta > NavArea?.AreaRect.End.x &&
                velocity.x > 0)
            {
                velocity.x = 0;
            }

            return MoveAndSlide(velocity, upDirection);
        }

        protected void FindGroundPosition()
        {
            Vector2 rightHitPos;
            // Cast left ray.
            _ray = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ShapeExtents.x, 0f),
                GlobalPosition + new Vector2(-ShapeExtents.x, _groundRayLengthForChar),
                new Array {this},
                GroundCollisionMask
            );
            if (_ray.Count > 0) // When left ray hits.
            {
                Vector2 leftHitPos = (Vector2) _ray["position"]; // Left ray hit position.

                // Cast right ray.
                _ray = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    GlobalPosition + new Vector2(ShapeExtents.x, _groundRayLengthForChar),
                    new Array {this},
                    GroundCollisionMask
                );
                if (_ray.Count > 0) // When both rays hit.
                {
                    rightHitPos = (Vector2) _ray["position"]; // Right ray hit position.

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
                _ray = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    GlobalPosition + new Vector2(ShapeExtents.x, _groundRayLengthForChar),
                    new Array {this},
                    GroundCollisionMask
                );
                if (_ray.Count > 0) // When only right ray hits. 
                {
                    rightHitPos = (Vector2) _ray["position"]; // Right ray hit position.

                    NavPos = rightHitPos + new Vector2(-ShapeExtents.x, 0);
                }
                else // When any rays do not hit.
                {
                    NavPos = GlobalPosition;
                }
            }
            
            NavChar.GlobalPosition = NavPos;
        }
        
        private void OnBodyEntered(Node node)
        {
            if (!(node is KinematicBody2D body)) return;
            _isColliding = true;
            _collidingBody = body;
        }
        
        private void OnBodyExited(Node node)
        {
            if (!(node is KinematicBody2D body)) return;
            if (body != _collidingBody) return;
            _isColliding = false;
            _collidingBody = null;
        }
        
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
