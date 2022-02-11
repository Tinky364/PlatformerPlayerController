using Godot;
using Godot.Collections;
using NavTool;

namespace AI
{
    public abstract class Enemy : Node2D
    {
        public StateMachine<EnemyStates> Fsm { get; } = new StateMachine<EnemyStates>();
        public NavBody2D NavBody;
        public AnimatedSprite AnimatedSprite;

        [Export]
        public bool DebugEnabled { get; private set; }
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity = 500f;
        [Export(PropertyHint.Range, "0.1,20")] 
        private float _groundDetectionHeight = 0.1f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        public float MoveSpeed = 15f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _damageValue = 1;
        
        public enum EnemyStates { Idle, Chase, Attack }
        private Dictionary _groundRay;
        public Vector2 Velocity = new Vector2();
        public Vector2 GroundHitPos { get; private set; }
        public int Direction { get; set; }
        private bool _hasEnabled = true;
        public bool HasEnabled
        {
            get => _hasEnabled;
            set
            {
                _hasEnabled = value;
                Visible = value;
                SetProcess(value);
                SetPhysicsProcess(value);
                foreach (Node child in GetChildren())
                {
                    child.SetProcess(value);
                    child.SetPhysicsProcess(value);
                }
            }
        }

        public override void _Ready()
        {
            AnimatedSprite = GetNode<AnimatedSprite>("NavBody2D/AnimatedSprite");
            NavBody = GetNode<NavBody2D>("NavBody2D");
            
            NavBody.NavArea.Connect("ScreenEntered", this, nameof(OnScreenEntered));
            NavBody.NavArea.Connect("ScreenExited", this, nameof(OnScreenExited));
        }

        public override void _Process(float delta)
        {
            StateController();
            Fsm._Process(delta);
            DirectionControl();
        }
        
        public override void _PhysicsProcess(float delta)
        {
            CheckGround();
            Fsm._PhysicsProcess(delta);

            if (!NavBody.IsOnGround)
            {
                Velocity.x = 0f;
                Velocity.y += Gravity * delta; // Adds gravity force increasingly.
            }
            Velocity = NavBody.MoveAndSlideInArea(Velocity, delta, Vector2.Up);
        }

        protected abstract void StateController();
        
        private void CheckGround()
        {
            // Raycast from the left bottom corner of the player.
            _groundRay = NavBody.GetWorld2d().DirectSpaceState.IntersectRay(
                NavBody.GlobalPosition + new Vector2(-NavBody.ShapeExtents.x, -2f), 
                NavBody.GlobalPosition + new Vector2(-NavBody.ShapeExtents.x, _groundDetectionHeight), 
                new Array {this},
                NavBody.CollisionMask
            );
            if (_groundRay.Count > 0)
            {
                GroundHitPos = (Vector2) _groundRay["position"] + new Vector2(NavBody.ShapeExtents.x, 0);
                NavBody.IsOnGround = true;
                return;
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner of the player.
            _groundRay = NavBody.GetWorld2d().DirectSpaceState.IntersectRay(
                NavBody.GlobalPosition + new Vector2(NavBody.ShapeExtents.x, -2f),
                NavBody.GlobalPosition + new Vector2(NavBody.ShapeExtents.x, _groundDetectionHeight),
                new Array {this},
                NavBody.CollisionMask
            );
            if (_groundRay.Count > 0)
            {
                GroundHitPos = (Vector2) _groundRay["position"] + new Vector2(-NavBody.ShapeExtents.x, 0);
                NavBody.IsOnGround = true;
                return;
            }
            // If raycasts do not hit the ground.
            NavBody.IsOnGround = false;
        }

        private void DirectionControl()
        {
            if (Velocity.x > 0)
                Direction = 1;
            else if (Velocity.x < 0)
                Direction = -1;

            switch (Direction)
            {
                case 1:
                    AnimatedSprite.FlipH = false;
                    break;
                case -1:
                    AnimatedSprite.FlipH = true;
                    break;
            }
        }

        private void OnScreenEntered()
        {
            HasEnabled = true;
        }

        private void OnScreenExited()
        {
            HasEnabled = false;
        }
    }
}
