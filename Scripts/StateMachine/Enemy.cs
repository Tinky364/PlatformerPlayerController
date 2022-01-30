using Godot;
using Godot.Collections;
using PlatformerPlayerController.Scripts.Navigation;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class Enemy : Node
    {
        [Export(PropertyHint.Range, "0.1,20")] 
        private float _groundDetectionHeight = 0.1f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        public float MoveSpeed = 15f;
        [Export(PropertyHint.Range, "0,100,or_greater")]
        public float StopDistance = 26f;
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity = 500f;
        
        public StateMachine<EnemyStates> Machine { get; } = new StateMachine<EnemyStates>();
        public KinematicBody2D Body;
        public CollisionShape2D Shape;
        public AnimatedSprite AnimatedSprite;
        public NavArea NavArea;
        public NavBody NavBody;
        
        public Vector2 Velocity = new Vector2();
        public Vector2 ShapeExtents { get; private set; }
        private Vector2 _groundHitPos = new Vector2();

        public bool IsOnGround { get; private set; }
        
        private Dictionary _groundRaycast;

        public override void _Ready()
        {
            Body = GetNode<KinematicBody2D>("Body");
            Shape = GetNode<CollisionShape2D>("Body/Shape");
            AnimatedSprite = GetNode<AnimatedSprite>("Body/AnimatedSprite");
            NavArea = GetNode<NavArea>("NavArea");
            NavBody = GetNode<NavBody>("Body/NavBody");
            if (Shape.Shape is RectangleShape2D shape)
                ShapeExtents = shape.Extents;

            Machine.AddState(new IdleState(this));
            Machine.AddState(new ChaseState(this));
            Machine.AddState(new AttackState(this));
            Machine.SetCurrentState(EnemyStates.Idle);
        }

        public override void _Process(float delta)
        {
            AnimationControl();
            
            Machine._Process(delta);
        }

        public override void _PhysicsProcess(float delta)
        {
            CheckGround();
            
            Machine._PhysicsProcess(delta);

            if (!IsOnGround)
            {
                Velocity.y += Gravity * delta; // Adds gravity force increasingly.
            }

            Velocity = Body.MoveAndSlide(Velocity, Vector2.Up);
        }

        private void OnTargetEntered(NavBody navBody)
        {
            Machine.SetCurrentState(EnemyStates.Chase);
        }
        
        private void OnTargetExited(NavBody navBody)
        {
            Machine.SetCurrentState(EnemyStates.Idle);
        }

        public void CheckGround()
        {
            // Raycast from the left bottom corner of the player.
            _groundRaycast = Body.GetWorld2d().DirectSpaceState.IntersectRay(
                Body.Position + new Vector2(-ShapeExtents.x, -2f), 
                Body.Position + new Vector2(-ShapeExtents.x, _groundDetectionHeight), 
                new Array {this},
                Body.CollisionMask
            );
            if (_groundRaycast.Count > 0)
            {
                _groundHitPos = (Vector2) _groundRaycast["position"] + new Vector2(ShapeExtents.x, 0);
                IsOnGround = true;
                return;
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner of the player.
            _groundRaycast = Body.GetWorld2d().DirectSpaceState.IntersectRay(
                Body.Position + new Vector2(ShapeExtents.x, -2f),
                Body.Position + new Vector2(ShapeExtents.x, _groundDetectionHeight),
                new Array {this},
                Body.CollisionMask
            );
            if (_groundRaycast.Count > 0)
            {
                _groundHitPos = (Vector2) _groundRaycast["position"] + new Vector2(-ShapeExtents.x, 0);
                IsOnGround = true;
                return;
            }
            // If raycasts do not hit the ground.
            IsOnGround = false;
        }

        public void AnimationControl()
        {
            if (IsOnGround)
            {
                if (Velocity.x < 0)
                {
                    AnimatedSprite.FlipH = true;
                    AnimatedSprite.Play("run");
                }
                else if (Velocity.x > 0)
                {
                    AnimatedSprite.FlipH = false;
                    AnimatedSprite.Play("run");
                }
                else
                {
                    AnimatedSprite.Play("idle");
                }
            }
        }
        
        public enum EnemyStates
        {
            Idle,
            Chase,
            Attack
        }
    }
}
