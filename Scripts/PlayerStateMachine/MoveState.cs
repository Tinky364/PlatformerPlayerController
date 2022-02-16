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

        public override void Enter()
        {
            GD.Print($"{P.Name}: {Key}");
        }

        public override void Process(float delta){ }

        public override void PhysicsProcess(float delta)
        {
            P.AxisInputs();
            _desiredMoveSpeedX = _moveSpeedX * P.InputAxis.x;

            if (Input.IsActionJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }
            
            // While the player is walking on the ground.
            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, _desiredMoveSpeedX, _moveAccelerationX * delta);
            P.Velocity.y = 500f * delta;
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, Vector2.Down * 2f, Vector2.Up);
            
            if (!P.IsOnFloor())
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }

        public override void Exit()
        {
            _desiredMoveSpeedX = 0;
        }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Move);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}
