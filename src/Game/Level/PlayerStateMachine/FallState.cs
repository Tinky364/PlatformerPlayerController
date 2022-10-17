using CustomRegister;
using Game.Fsm;
using Godot;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.PlayerStateMachine
{
    [Register]
    public class FallState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "0.01,1,0.005,or_greater")]
        private float _afterLeavingGroundJumpAbleDur = 0.08f;
        [Export(PropertyHint.Range, "0.1,20,0.05,or_greater")] 
        private float _beforeHitGroundJumpAbleRayLength = 5f;
        
        private float _desiredSpeedX;
        private bool _isAfterLeavingGroundJumpAble;

        public override void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            Owner.SnapDisabled = true;
            Owner.PlayAnimation(Mathf.Abs(Owner.Velocity.x) > 30f ? "fall_side" : "fall_down");
            if (Owner.Fsm.PreviousState?.Key == Player.PlayerStates.Move)
                CalculateAfterLeavingGroundJumpAble();
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            Owner.CastWallRay();

            if ((_isAfterLeavingGroundJumpAble || CalculateBeforeHitGroundJumpAble()) &&
                InputInvoker.IsPressed("jump"))
            {
                Owner.DashState.SetDashSettings(true);
                Owner.Fsm.ChangeState(Player.PlayerStates.Jump);
                return;
            }
            
            if (!Owner.DashState.DashUnable && InputInvoker.IsPressed("dash"))
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Dash);
                return;
            }
            
            if (Owner.IsWallJumpAble && InputInvoker.IsPressed("jump"))
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.WallJump);
                return;
            }

            if (Owner.IsCollidingWithPlatform && !Owner.FallOffPlatformInput)
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Platform);
                return;
            }

            if (Owner.IsOnFloor())
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Move);
                return;
            }

            if (Owner.IsStayOnWall)
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Wall);
                return;
            }

            Owner.PlayAnimation(Mathf.Abs(Owner.Velocity.x) > 30f ? "fall_side" : "fall_down");

            _desiredSpeedX = (Owner.JumpState.SpeedX - 15f) * Owner.AxisInputs().x;
            Owner.Velocity.x = Mathf.MoveToward(
                Owner.Velocity.x, _desiredSpeedX, Owner.AirAccelerationX * delta
            );
            if (Owner.Velocity.y < Owner.GravitySpeedMax) Owner.Velocity.y += Owner.Gravity * delta;
        }
        
        public override void Process(float delta) { }
        public override void Exit() { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }

        private async void CalculateAfterLeavingGroundJumpAble()
        {
            _isAfterLeavingGroundJumpAble = true;
            await TreeTimer.Singleton.Wait(_afterLeavingGroundJumpAbleDur);
            _isAfterLeavingGroundJumpAble = false;
        }

        private bool CalculateBeforeHitGroundJumpAble() =>
            Owner.IsGroundRayHit &&
            Owner.NavPos.DistanceTo(Owner.GlobalPosition) <= _beforeHitGroundJumpAbleRayLength;
    }
}
