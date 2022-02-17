using Godot;
using AI;

namespace PlayerStateMachine
{
    public class MoveState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _moveAccelerationX = 400f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _moveSpeedX = 70f;

        private float _desiredMoveSpeedX;
        private bool _isFallingFromPlatform;

        public override void Enter()
        {
            if (P.DebugEnabled) GD.Print($"{P.Name}: {Key}");
        }

        public override void Process(float delta)
        {
            P.AnimSprite.Play(P.Velocity.x == 0 ? "idle" : "run");
        }

        public override void PhysicsProcess(float delta)
        {
            if (Input.IsActionJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }

           
            if (DropFromPlatform() && !P.IsOnFloor())
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
            // While the player is walking on the ground.
            _desiredMoveSpeedX = _moveSpeedX * P.AxisInputs().x;
            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, _desiredMoveSpeedX, _moveAccelerationX * delta);
            P.Velocity.y = P.Gravity * delta;
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, Vector2.Down * 2f, Vector2.Up);
            
            if (!P.IsOnFloor()) 
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }

        public override void Exit() { }

        private bool DropFromPlatform()
        {
            if (P.GroundRay.Count <= 0 || !(P.GroundRay["collider"] is CollisionObject2D ground))
                return false;
            if (ground.CollisionLayer != P.PlatformMask || !Input.IsActionJustPressed("move_down"))
                return false;
            _isFallingFromPlatform = true;
            P.SetCollisionMaskBit(2, false);
            P.GroundMask -= P.PlatformMask;
            return true;
        }

        private void OnPlatformExited(Node body)
        {
            if (!_isFallingFromPlatform) return;
            _isFallingFromPlatform = false;
            P.SetCollisionMaskBit(2, true); 
            P.GroundMask += P.PlatformMask;
        }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Move);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}
