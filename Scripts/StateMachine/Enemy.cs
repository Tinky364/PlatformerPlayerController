using Godot;
using Godot.Collections;
using PlatformerPlayerController.Scripts.Navigation;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class Enemy : Node
    {
        public StateMachine<EnemyStates> Fsm { get; } = new StateMachine<EnemyStates>();
        private KinematicBody2D _body;
        public NavArea2D NavArea;
        public NavChar2D NavChar;
        public AnimatedSprite AnimatedSprite;
        private Area2D _triggerArea;
        private CollisionShape2D _shape;

        [Export]
        public bool HasEnabled { get; private set; } = true;
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity = 500f;
        [Export(PropertyHint.Range, "0.1,20")] 
        private float _groundDetectionHeight = 0.1f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        public float MoveSpeed = 15f;
        [Export(PropertyHint.File, ".res")]
        private ChaseState _chaseState;
        [Export(PropertyHint.File, ".res")]
        private AttackState _attackState;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _damageValue = 1;
        
        public enum EnemyStates { Idle, Chase, Attack }
        private Dictionary _groundRay;
        public Vector2 Velocity = new Vector2();
        public Vector2 ShapeExtents { get; private set; }
        public Vector2 GroundHitPos { get; private set; }
        public int Direction { get; set; }
        public bool IsOnGround { get; private set; }
        public bool CanAttack { get; set; }

        public override void _Ready()
        {
            SetProcess(HasEnabled);
            SetPhysicsProcess(HasEnabled);
            
            _body = GetNode<KinematicBody2D>("Body");
            _shape = GetNode<CollisionShape2D>("Body/Shape");
            AnimatedSprite = GetNode<AnimatedSprite>("Body/AnimatedSprite");
            NavArea = GetNode<NavArea2D>("NavArea");
            NavChar = GetNode<NavChar2D>("Body/NavChar");
            _triggerArea = GetNode<Area2D>("Body/TriggerArea");
            if (_shape.Shape is RectangleShape2D shape) ShapeExtents = shape.Extents;

            new IdleState().Initialize(this);
            _attackState.Initialize(this);
            _chaseState.Initialize(this);
            
            _triggerArea.Connect("body_entered", this, nameof(OnBodyEntered));

            Fsm.SetCurrentState(EnemyStates.Idle);
        }

        
        public override void _Process(float delta)
        {
            if (!NavArea.IsOnCam) return;
            
            StateController();
            Fsm._Process(delta);
            
            DirectionControl();
        }

        private void StateController()
        {
            if (!NavArea.IsTargetReachable)
            {
                Fsm.SetCurrentState(EnemyStates.Idle);
                return;
            }
            if (StateAccordingToPosition(_chaseState.StopDistance)) return;
            if (StateAccordingToPosition(-_chaseState.StopDistance)) return;
            Fsm.SetCurrentState(EnemyStates.Idle);
        }

        private bool StateAccordingToPosition(float stopDistance)
        {
            Vector2 dirToTarget = NavArea.DirectionToTarget();
            if (dirToTarget == Vector2.Zero) dirToTarget = Vector2.Right;
            Vector2 targetPos = NavArea.TargetNavChar.NavPosition + dirToTarget * -stopDistance;
            
            if (!NavArea.IsPositionInArea(targetPos)) return false;
            
            _chaseState.TargetPos = targetPos;
            float distance = NavChar.NavPosition.DistanceTo(targetPos);
            if (distance < 2f && distance > -2f) Fsm.SetCurrentState(EnemyStates.Attack);
            else Fsm.SetCurrentState(EnemyStates.Chase);
            return true;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (!NavArea.IsOnCam) return;
            
            CheckGround();
            Fsm._PhysicsProcess(delta);

            if (!IsOnGround)
            {
                Velocity.x = 0f;
                Velocity.y += Gravity * delta; // Adds gravity force increasingly.
            }
            
            Velocity = NavChar.MoveAndSlide(Velocity, delta, Vector2.Up);
        }

        private void CheckGround()
        {
            // Raycast from the left bottom corner of the player.
            _groundRay = _body.GetWorld2d().DirectSpaceState.IntersectRay(
                _body.Position + new Vector2(-ShapeExtents.x, -2f), 
                _body.Position + new Vector2(-ShapeExtents.x, _groundDetectionHeight), 
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
                _body.Position + new Vector2(ShapeExtents.x, -2f),
                _body.Position + new Vector2(ShapeExtents.x, _groundDetectionHeight),
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
            if (CanAttack)
                Events.Singleton.EmitSignal("Damaged", body, _damageValue, this);
        }
    }
}
