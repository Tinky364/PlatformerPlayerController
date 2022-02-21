using Godot;
using Godot.Collections;

namespace NavTool
{
    public class NavBody2D : KinematicBody2D
    {
        [Export]
        public NavBodyType CurNavBodyType { get; private set; } = NavBodyType.Platformer;
        [Export(PropertyHint.Range, "0,500,1,or_greater")]
        public Vector2 Extents { get; private set; }
        [Export(PropertyHint.Layers2dPhysics)]
        public uint GroundLayer { get; set; } = 6;
        [Export]
        private bool _isOnBodyCollidingActive;
        [Export(PropertyHint.Range, "0.1,200,or_greater")] 
        private float _groundRayLength = 35f;
        
        [Signal]
        protected delegate void BodyEntered(Node body);
        [Signal]
        protected delegate void BodyColliding(Node body);
        [Signal]
        protected delegate void BodyExited(Node body);

        public NavTween NavTween { get; private set; }
        private Area2D _interactionArea;
        
        public enum NavBodyType { Platformer, TopDown }
        public Node Ground { get; private set; }
        public Vector2 NavPos { get; private set; }
        public Vector2 ExtentsHalf => Extents / 2f;
        public Vector2 Velocity;
        public Vector2 Direction = new Vector2(1, 0);
        public uint CurGroundLayer { get; private set; }
        public bool IsInactive => !Visible;
        public bool IsUnhurtable { get; set; }
        public bool IsDead { get; protected set; }
        public bool IsGroundRayHit { get; private set; }
        protected PhysicsBody2D CollidingBody { get; private set; }
        protected Physics2DDirectSpaceState SpaceState { get; private set; }
        private Dictionary _groundRay;
        private bool _isColliding;

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
                    NavPos = CastGroundRay();
                    break;
                case NavBodyType.TopDown:
                    NavPos = GlobalPosition;
                    break;
            }
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
            if (!_isOnBodyCollidingActive) return;
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

        private Vector2 CastGroundRay()
        {
            Vector2 leftHitPos = new Vector2();
            Vector2 rightHitPos = new Vector2();
            bool isLeftHit = false;
            bool isRightHit = false;
            // Raycast from the left bottom corner.
            _groundRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ExtentsHalf.x, -5f),
                GlobalPosition + new Vector2(-ExtentsHalf.x, _groundRayLength + 5f),
                new Array {this}, GroundLayer
            );
            if (_groundRay.Count > 0)
            {
                IsGroundRayHit = true;
                isLeftHit = true;
                leftHitPos = (Vector2) _groundRay["position"] + new Vector2(ExtentsHalf.x, 0f);
                Ground = _groundRay["collider"] as Node;
                switch (Ground)
                {
                    case CollisionObject2D collision: CurGroundLayer = collision.CollisionLayer;
                        break;
                    case TileMap tile: CurGroundLayer = tile.CollisionLayer;
                        break;
                }
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner.
            _groundRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ExtentsHalf.x, -5f),
                GlobalPosition + new Vector2(ExtentsHalf.x, _groundRayLength + 5f), new Array {this},
                GroundLayer
            );
            if (_groundRay.Count > 0)
            {
                IsGroundRayHit = true;
                isRightHit = true;
                rightHitPos = (Vector2) _groundRay["position"] + new Vector2(-ExtentsHalf.x, 0f);
            }

            if (isLeftHit && isRightHit)
            {
                if (leftHitPos == rightHitPos) return leftHitPos;
                if (leftHitPos.DistanceSquaredTo(GlobalPosition) <
                    rightHitPos.DistanceSquaredTo(GlobalPosition)) return leftHitPos;
                
                Ground = _groundRay["collider"] as Node;
                switch (Ground)
                {
                    case CollisionObject2D collision: CurGroundLayer = collision.CollisionLayer;
                        break;
                    case TileMap tile: CurGroundLayer = tile.CollisionLayer;
                        break;
                }
                return rightHitPos;
            }
            if (isLeftHit) return leftHitPos;
            if (isRightHit)
            {
                Ground = _groundRay["collider"] as Node;
                switch (Ground)
                {
                    case CollisionObject2D collision: CurGroundLayer = collision.CollisionLayer;
                        break;
                    case TileMap tile: CurGroundLayer = tile.CollisionLayer;
                        break;
                }
                return rightHitPos;
            }

            CurGroundLayer = 0;
            IsGroundRayHit = false;
            Ground = null;
            return GlobalPosition;
        }
    }
}
