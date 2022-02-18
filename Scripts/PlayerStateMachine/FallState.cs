using Godot;
using AI;

namespace PlayerStateMachine
{
    public class FallState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _airAccelerationX = 600f;
        [Export(PropertyHint.Range, "0.01,5,0.05,or_greater")]
        private float _jumpAbleDur = 0.1f;
        
        private float _desiredAirSpeedX;
        private bool _jumpAble;

        public override void Enter()
        {
            if (P.DebugEnabled) GD.Print($"{P.Name}: {Key}");
            P.AnimPlayer.Play("fall");
            if (P.Fsm.PreviousState?.Key != Player.PlayerStates.Jump)
                StartJumpAbleDuration();
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            _desiredAirSpeedX = P.JumpState.JumpSpeedX * P.AxisInputs().x;

            if (_jumpAble || P.GroundRay.Count > 0)
            {
                if (Input.IsActionJustPressed("jump"))
                {
                    P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                    return;
                }
            }
            
            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, _desiredAirSpeedX, _airAccelerationX * delta);
            if (P.Velocity.y < P.GravitySpeedMax)
                P.Velocity.y += P.Gravity * delta;
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, Vector2.Zero, Vector2.Up);
            
            if (P.IsOnFloor())
                P.Fsm.SetCurrentState(Player.PlayerStates.Move);
        }

        public override void Exit() { }
        
        private async void StartJumpAbleDuration()
        {
            _jumpAble = true;
            await ToSignal(P.GetTree().CreateTimer(_jumpAbleDur), "timeout");
            _jumpAble = false;
        }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Fall);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}
