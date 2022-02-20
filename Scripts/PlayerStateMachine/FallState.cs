using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class FallState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _airAccelerationX = 600f;
        [Export(PropertyHint.Range, "0.01,5,0.05,or_greater")]
        private float _afterLeavingGroundJumpAbleDur = 0.1f;
        [Export(PropertyHint.Range, "0.1,20,0.05,or_greater")] 
        private float _beforeHitGroundJumpAbleRayLength = 5f;
        
        private float _desiredAirSpeedX;
        private bool _isAfterLeavingGroundJumpAble;

        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.AnimPlayer.Play("fall");
            if (P.Fsm.PreviousState?.Key == Player.PlayerStates.Move)
                CalculateAfterLeavingGroundJumpAble();
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (_isAfterLeavingGroundJumpAble || CalculateBeforeHitGroundJumpAble())
            {
                if (Input.IsActionJustPressed("jump"))
                {
                    P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                    return;
                }
            }

            if (P.IsCollidingWithPlatform && !P.FallOffPlatformInput)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Platform);
                return;
            }

            if (P.IsOnFloor())
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                return;
            }

            _desiredAirSpeedX = P.JumpState.JumpSpeedX * P.AxisInputs().x;
            P.Velocity.x = Mathf.MoveToward(
                P.Velocity.x,
                _desiredAirSpeedX,
                _airAccelerationX * delta
            );
            if (P.Velocity.y < P.GravitySpeedMax) P.Velocity.y += P.Gravity * delta;
        }

        public override void Exit() { }
        
        private async void CalculateAfterLeavingGroundJumpAble()
        {
            _isAfterLeavingGroundJumpAble = true;
            await TreeTimer.S.Wait(_afterLeavingGroundJumpAbleDur);
            _isAfterLeavingGroundJumpAble = false;
        }

        private bool CalculateBeforeHitGroundJumpAble() =>
            P.IsGroundRayHit && 
            P.NavPos.DistanceTo(P.GlobalPosition) <= _beforeHitGroundJumpAbleRayLength;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Fall);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}
