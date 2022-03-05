using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class FallState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "0.01,1,0.005,or_greater")]
        private float _afterLeavingGroundJumpAbleDur = 0.08f;
        [Export(PropertyHint.Range, "0.1,20,0.05,or_greater")] 
        private float _beforeHitGroundJumpAbleRayLength = 5f;
        
        private Player P { get; set; }

        private float _desiredSpeedX;
        private bool _isAfterLeavingGroundJumpAble;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Fall);
            P = player;
            P.Fsm.AddState(this);
        }
        
        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.PlayAnim(Mathf.Abs(P.Velocity.x) > 30f ? "fall_side" : "fall_down");
            if (P.Fsm.PreviousState?.Key == Player.PlayerStates.Move)
                CalculateAfterLeavingGroundJumpAble();
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            P.CastWallRay();

            if ((_isAfterLeavingGroundJumpAble || CalculateBeforeHitGroundJumpAble()) &&
                InputManager.IsJustPressed("jump"))
            {
                P.DashState.SetDashSettings(true);
                P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }
            
            if (!P.DashState.DashUnable && InputManager.IsJustPressed("dash"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Dash);
                return;
            }
            
            if (P.IsWallJumpAble && InputManager.IsJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.WallJump);
                return;
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

            if (P.IsStayOnWall)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Wall);
                return;
            }

            P.PlayAnim(Mathf.Abs(P.Velocity.x) > 30f ? "fall_side" : "fall_down");

            _desiredSpeedX = (P.JumpState.SpeedX - 15f) * P.AxisInputs().x;
            P.Velocity.x = Mathf.MoveToward(
                P.Velocity.x, _desiredSpeedX, P.AirAccelerationX * delta
            );
            if (P.Velocity.y < P.GravitySpeedMax) P.Velocity.y += P.Gravity * delta;
        }
        
        public override void Process(float delta) { }

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
    }
}
