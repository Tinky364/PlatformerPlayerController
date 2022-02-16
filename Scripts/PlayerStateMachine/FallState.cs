using Godot;
using AI;

namespace PlayerStateMachine
{
    public class FallState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _airAccelerationX = 600f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _airSpeedX = 70f;
        
        private float _desiredAirSpeedX;

        public override void Enter()
        {
            GD.Print($"{P.Name}: {Key}");
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.AxisInputs();
            _desiredAirSpeedX = _airSpeedX * P.InputAxis.x;
            
            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, _desiredAirSpeedX, _airAccelerationX * delta);
            if (P.Velocity.y < P.GravitySpeedMax)
                P.Velocity.y += P.Gravity * delta;
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, Vector2.Zero, Vector2.Up);
            
            if (P.IsOnFloor())
                P.Fsm.SetCurrentState(Player.PlayerStates.Move);
        }

        public override void Exit()
        {
            _desiredAirSpeedX = 0;
        }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Fall);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}
