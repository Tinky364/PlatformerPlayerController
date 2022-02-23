using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class FallState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "0.01,5,0.05,or_greater")]
        private float _afterLeavingGroundJumpAbleDur = 0.1f;
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
            P.AnimPlayer.Play("fall");
            if (P.Fsm.PreviousState?.Key == Player.PlayerStates.Move)
                CalculateAfterLeavingGroundJumpAble();
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            P.CastWallRay();

            if ((_isAfterLeavingGroundJumpAble || CalculateBeforeHitGroundJumpAble()) &&
                Input.IsActionJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }
            
            if (P.IsWallRayHit && Input.IsActionJustPressed("jump"))
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

            if (P.IsWallRayHit && P.IsOnWall)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Wall);
                return;
            }

            _desiredSpeedX = (P.JumpState.SpeedX - 10) * P.AxisInputs().x;
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
