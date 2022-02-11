using Godot;
using Godot.Collections;
using NavTool;

namespace AI
{
    public abstract class Enemy : Node2D
    {
        public StateMachine<EnemyStates> Fsm { get; } = new StateMachine<EnemyStates>();
        private KinematicBody2D _body;
        public NavArea2D NavArea;
        public NavChar2D NavChar;
        public AnimatedSprite AnimatedSprite;
        private Area2D _triggerArea;
        private CollisionShape2D _shape;

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
        
        private Node _collidingBody;
        public enum EnemyStates { Idle, Chase, Attack }
        private Dictionary _groundRay;
        public Vector2 Velocity = new Vector2();
        public Vector2 ShapeExtents { get; private set; }
        public Vector2 GroundHitPos { get; private set; }
        public int Direction { get; set; }
        public bool IsOnGround { get; private set; }
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
        private bool _isColliding;

        public override void _Ready()
        {
            _body = GetNode<KinematicBody2D>("Body");
            _shape = GetNode<CollisionShape2D>("Body/Shape");
            AnimatedSprite = GetNode<AnimatedSprite>("Body/AnimatedSprite");
            NavArea = GetNode<NavArea2D>("NavArea");
            NavChar = GetNode<NavChar2D>("Body/NavChar");
            _triggerArea = GetNode<Area2D>("Body/TriggerArea");
            if (_shape.Shape is RectangleShape2D shape) ShapeExtents = shape.Extents;
            _triggerArea.Connect("body_entered", this, nameof(OnBodyEntered));
            _triggerArea.Connect("body_exited", this, nameof(OnBodyExited));
            NavArea.Connect("ScreenEntered", this, nameof(OnScreenEntered));
            NavArea.Connect("ScreenExited", this, nameof(OnScreenExited));
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
            WhileColliding(_collidingBody);

            if (!IsOnGround)
            {
                Velocity.x = 0f;
                Velocity.y += Gravity * delta; // Adds gravity force increasingly.
            }
            Velocity = NavChar.MoveAndSlide(Velocity, delta, Vector2.Up);
        }

        protected abstract void StateController();

        private void WhileColliding(Node body)
        {
            if (!_isColliding) return;
            if (body is KinematicBody2D kinBody)
            {
                Events.Singleton.EmitSignal(
                    "Damaged",
                    body,
                    _damageValue,
                    this,
                    _body.GlobalPosition.DirectionTo(kinBody.GlobalPosition)
                );
            }
        }
        
        private void CheckGround()
        {
            // Raycast from the left bottom corner of the player.
            _groundRay = _body.GetWorld2d().DirectSpaceState.IntersectRay(
                _body.GlobalPosition + new Vector2(-ShapeExtents.x, -2f), 
                _body.GlobalPosition + new Vector2(-ShapeExtents.x, _groundDetectionHeight), 
                new Array {this},
                _body.CollisionMask
            );
            if (_groundRay.Count > 0)
            {
                GroundHitPos = (Vector2) _groundRay["position"] + new Vector2(ShapeExtents.x, 0);
                IsOnGround = true;
                return;
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner of the player.
            _groundRay = _body.GetWorld2d().DirectSpaceState.IntersectRay(
                _body.GlobalPosition + new Vector2(ShapeExtents.x, -2f),
                _body.GlobalPosition + new Vector2(ShapeExtents.x, _groundDetectionHeight),
                new Array {this},
                _body.CollisionMask
            );
            if (_groundRay.Count > 0)
            {
                GroundHitPos = (Vector2) _groundRay["position"] + new Vector2(-ShapeExtents.x, 0);
                IsOnGround = true;
                return;
            }
            // If raycasts do not hit the ground.
            IsOnGround = false;
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
        
        private void OnBodyEntered(Node body)
        {
            _isColliding = true;
            _collidingBody = body;
        }
        
        private void OnBodyExited(Node body)
        {
            _isColliding = false;
            _collidingBody = null;
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
