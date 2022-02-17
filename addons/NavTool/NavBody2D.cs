using Godot;
using Godot.Collections;

namespace NavTool
{
    public class NavBody2D : KinematicBody2D
    {
        public NavTween NavTween { get; private set; }
        private Area2D _interactionArea;
        protected PhysicsBody2D CollidingBody;

        [Export]
        public NavBodyType CurNavBodyType { get; private set; } = NavBodyType.Platformer;
        [Export]
        private bool IsOnBodyCollidingActive { get; set; }
        [Export(PropertyHint.Range, "0,500,1,or_greater")]
        public Vector2 Extents { get; private set; }
        [Export(PropertyHint.Range, "0.1,200,or_greater")] 
        private float NavPosRayLength { get; set; } = 35f;
        [Export(PropertyHint.Layers2dPhysics)]
        public uint GroundMask = 6;
        
        [Signal]
        protected delegate void BodyEntered(Node body);
        [Signal]
        protected delegate void BodyColliding(Node body);
        [Signal]
        protected delegate void BodyExited(Node body);

        public enum NavBodyType { Platformer, TopDown }
        protected Physics2DDirectSpaceState SpaceState;
        private Dictionary _navPosRay;
        public Vector2 NavPos { get; private set; }
        public Vector2 ExtentsHalf => Extents / 2f;
        public Vector2 Velocity;
        public Vector2 Direction = new Vector2(1, 0);
        private bool _isColliding;
        private bool _isInactive;
        public bool IsInactive
        {
            get => _isInactive;
            set
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
        public bool IsUnhurtable { get; set; }
        public bool IsDead { get; set; }

        public override void _EnterTree()
        {
            NavTween = new NavTween();
            AddChild(NavTween);
            NavTween.Name = "NavTween";
            NavTween.ConnectTween(this, "Velocity");
        }

        public override void _Ready()
        {
            SpaceState = GetWorld2d().DirectSpaceState;
            _interactionArea = GetNodeOrNull<Area2D>("Area2D");
            _interactionArea?.Connect("body_entered", this, nameof(OnBodyEntered));
            _interactionArea?.Connect("body_exited", this, nameof(OnBodyExited));
            SetNavPos();
        }

        public override void _PhysicsProcess(float delta)
        {
            SetNavPos();
            OnBodyColliding(CollidingBody);
        }

        public Vector2 DirectionTo(Vector2 to) => NavPos.DirectionTo(to);

        public float DistanceTo(Vector2 to) => NavPos.DistanceTo(to);

        public Vector2 Move(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (NavTween.IsPlaying) velocity = NavTween.EqualizeVelocity(velocity, delta);
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
                GlobalPosition + new Vector2(-ExtentsHalf.x, 0f),
                GlobalPosition + new Vector2(-ExtentsHalf.x, NavPosRayLength),
                new Array {this},
                GroundMask
            );
            // When left ray hits.
            if (_navPosRay.Count > 0) 
            {
                Vector2 leftHitPos = (Vector2) _navPosRay["position"]; // Left ray hit position.
                // Cast right ray.
                _navPosRay = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ExtentsHalf.x, 0f),
                    GlobalPosition + new Vector2(ExtentsHalf.x, NavPosRayLength),
                    new Array {this},
                    GroundMask
                );
                // When both rays hit.
                if (_navPosRay.Count > 0) 
                {
                    rightHitPos = (Vector2) _navPosRay["position"]; // Right ray hit position.
                    // Decides position according to the close hit position.
                    if (rightHitPos.DistanceTo(GlobalPosition) > leftHitPos.DistanceTo(GlobalPosition))
                        return leftHitPos + new Vector2(ExtentsHalf.x, 0);
                    return rightHitPos + new Vector2(-ExtentsHalf.x, 0);
                }
                // When only left ray hits.
                return leftHitPos + new Vector2(ExtentsHalf.x, 0);
            }
            // When left ray does not hit.
            // Cast right ray.
            _navPosRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ExtentsHalf.x, 0f),
                GlobalPosition + new Vector2(ExtentsHalf.x, NavPosRayLength),
                new Array {this},
                GroundMask
            );
            // When only right ray hits. 
            if (_navPosRay.Count > 0) 
            {
                rightHitPos = (Vector2) _navPosRay["position"]; // Right ray hit position.
                return rightHitPos + new Vector2(-ExtentsHalf.x, 0);
            }
            // When any rays do not hit.
            return GlobalPosition;
        }
        
        private void OnBodyEntered(Node node)
        {
            if (!(node is PhysicsBody2D body)) return;
            _isColliding = true;
            CollidingBody = body;
            EmitSignal(nameof(BodyEntered), node);
        }

        private void OnBodyColliding(Node body)
        {
            if (!IsOnBodyCollidingActive) return;
            if (!_isColliding) return;
            EmitSignal(nameof(BodyColliding), body);
        }

        private void OnBodyExited(Node node)
        {
            if (!(node is PhysicsBody2D body)) return;
            if (body != CollidingBody) return;
            _isColliding = false;
            CollidingBody = null;
            EmitSignal(nameof(BodyExited), node);
        }
    }
}
