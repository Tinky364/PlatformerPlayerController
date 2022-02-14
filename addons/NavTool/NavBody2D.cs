using Godot;
using Godot.Collections;

namespace NavTool
{
    public class NavBody2D : KinematicBody2D
    {
        public NavArea2D NavArea { get; private set; }
        public NavTween NavTween { get; private set; }
        private CollisionShape2D _shape;
        private Area2D _area;
        
        [Export]
        private NavBodyType CurNavBodyType { get; set; }  = NavBodyType.Platformer;
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

        public enum NavBodyType { Platformer, TopDown }
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
        public bool IsInactive
        {
            get => _isInactive;
            protected set
            {
                _isInactive = value;
                SetProcess(!value);
                SetPhysicsProcess(!value);
                Visible = !value;
                foreach (Node child in GetChildren())
                {
                    child.SetProcess(!value);
                    child.SetPhysicsProcess(!value);
                }
            }
        }
        protected bool IsUnhurtable { get; set; }
        protected bool IsOnGround { get; set; }
        protected bool HasGroundRayDisabled;
        private bool _isColliding;
        private bool _isInactive;

        public override void _EnterTree()
        {
            NavTween = new NavTween();
            AddChild(NavTween);
            NavTween.Name = "NavTween";
        }

        public override void _Ready()
        {
            NavTween.ConnectVelocityVariable("Velocity");
            _area = GetNodeOrNull<Area2D>("Area2D");
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            SpaceState = GetWorld2d().DirectSpaceState;
            if (_targetNavBodyPath != null) 
                TargetNavBody = GetNodeOrNull<NavBody2D>(_targetNavBodyPath);
            if (_shape.Shape is RectangleShape2D shape) 
                ShapeExtents = shape.Extents;
            if (_navAreaPath != null) 
                NavArea = GetNodeOrNull<NavArea2D>(_navAreaPath);
            if (NavArea != null && !NavArea.IsPositionInArea(GlobalPosition))
                GlobalPosition = NavArea.GlobalPosition;
            _area?.Connect("body_entered", this, nameof(OnBodyEntered));
            _area?.Connect("body_exited", this, nameof(OnBodyExited));
            NavArea?.Connect("ScreenEntered", this, nameof(OnScreenEnter));
            NavArea?.Connect("ScreenExited", this, nameof(OnScreenExit));
            SetNavPos();
        }

        public override void _PhysicsProcess(float delta)
        {
            NavArea?.CheckTargetInArea(TargetNavBody);
            if (CurNavBodyType == NavBodyType.Platformer)
                CastGroundRay();
            SetNavPos();
            OnBodyColliding(CollidingBody);
        }

        public Vector2 DirectionTo(Vector2 to) => NavPos.DirectionTo(to);

        public float DistanceTo(Vector2 to) => NavPos.DistanceTo(to);

        protected Vector2 MoveAndSlideInArea(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (NavTween.IsPlaying)
            {
                velocity = NavTween.EqualizeVelocity(velocity);
            }
            if (NavArea != null)
            {
                Vector2 nextFramePos = NavPos + velocity * delta;
                switch (CurNavBodyType)
                {
                    case NavBodyType.Platformer:
                        if (nextFramePos.x < NavArea.AreaRect.Position.x && velocity.x < 0 || nextFramePos.x > NavArea.AreaRect.End.x && velocity.x > 0)
                            velocity.x = 0;
                        break;
                    case NavBodyType.TopDown:
                        if (nextFramePos.x < NavArea.AreaRect.Position.x && velocity.x < 0 || nextFramePos.x > NavArea.AreaRect.End.x && velocity.x > 0)
                            velocity.x = 0;
                        if (nextFramePos.y < NavArea.AreaRect.Position.y && velocity.y < 0 || nextFramePos.y > NavArea.AreaRect.End.y && velocity.y > 0)
                            velocity.y = 0;
                        break;
                }
            }
            return MoveAndSlide(velocity, upDirection);
        }

        private void SetNavPos()
        {
            switch (CurNavBodyType)
            {
                case NavBodyType.Platformer:
                    NavPos = CastNavPosRay();
                    break;
                case NavBodyType.TopDown:
                    NavPos = GlobalPosition;
                    break;
            }
        }

        private Vector2 CastNavPosRay()
        {
            Vector2 rightHitPos;
            // Cast left ray.
            _navPosRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ShapeExtents.x, 0f),
                GlobalPosition + new Vector2(-ShapeExtents.x, _navPosRayLength),
                new Array {this},
                GroundCollisionMask
            );
            // When left ray hits.
            if (_navPosRay.Count > 0) 
            {
                Vector2 leftHitPos = (Vector2) _navPosRay["position"]; // Left ray hit position.
                // Cast right ray.
                _navPosRay = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    GlobalPosition + new Vector2(ShapeExtents.x, _navPosRayLength),
                    new Array {this},
                    GroundCollisionMask
                );
                // When both rays hit.
                if (_navPosRay.Count > 0) 
                {
                    rightHitPos = (Vector2) _navPosRay["position"]; // Right ray hit position.
                    // Decides position according to the close hit position.
                    if (rightHitPos.DistanceTo(GlobalPosition) > leftHitPos.DistanceTo(GlobalPosition))
                        return leftHitPos + new Vector2(ShapeExtents.x, 0);
                    return rightHitPos + new Vector2(-ShapeExtents.x, 0);
                }
                // When only left ray hits.
                return leftHitPos + new Vector2(ShapeExtents.x, 0);
            }
            // When left ray does not hit.
            // Cast right ray.
            _navPosRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                GlobalPosition + new Vector2(ShapeExtents.x, _navPosRayLength),
                new Array {this},
                GroundCollisionMask
            );
            // When only right ray hits. 
            if (_navPosRay.Count > 0) 
            {
                rightHitPos = (Vector2) _navPosRay["position"]; // Right ray hit position.
                return rightHitPos + new Vector2(-ShapeExtents.x, 0);
            }
            // When any rays do not hit.
            return GlobalPosition;
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
                GlobalPosition.DirectionTo(targetNavBody.GlobalPosition)
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
        
        public Vector2 DirectionToTarget()
        {
            if (TargetNavBody == null) return Vector2.Zero;
            Vector2 dir = (TargetNavBody.NavPos - NavPos).Normalized();
            if (dir == Vector2.Zero) dir = Vector2.Right;
            return dir;
        } 

        public float DistanceToTarget()
        { 
            if (TargetNavBody == null) return 0;
            return (TargetNavBody.NavPos - NavPos).Length();
        }

        protected virtual void OnScreenEnter() => IsInactive = false;

        protected virtual void OnScreenExit() => IsInactive = true;

        protected bool IsGroundAngleEnough(float groundAngle, float limit)
            => groundAngle > -limit && groundAngle < limit;
    
        protected float CalculateGroundAngle(Vector2 normal) 
            => Mathf.Rad2Deg(normal.AngleTo(Vector2.Up));
    }
}