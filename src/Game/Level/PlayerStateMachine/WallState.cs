using CustomRegister;
using Godot;
using Game.Fsm;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.PlayerStateMachine
{
    [Register]
    public class WallState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationY = 60f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        private float _speedMaxY = 100f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _slideSpeedYNeg = 5f;
        [Export(PropertyHint.Range, "0,1,0.05,or_greater")]
        private float _fallDelayDuration = 0.25f;
        
        private float _fallDelayCount;

        public override void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            _fallDelayCount = 0f;
            Owner.SnapDisabled = true;
            Owner.PlayAnimation("wall_landing");
            Owner.AnimPlayer.Queue("wall_slide");
            Owner.Velocity.x = 70f * Owner.WallDirection.x;
            if (Owner.Velocity.y > 0f) Owner.Velocity.y = 0f;
            else Owner.Velocity.y = -_slideSpeedYNeg;
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.WallDirection * 2f, Vector2.Up);

            Owner.CastWallRay();

            if (Owner.IsWallJumpAble && InputInvoker.IsPressed("jump"))
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.WallJump);
                return;
            }
            
            if (!Owner.DashState.DashUnable && InputInvoker.IsPressed("dash"))
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Dash);
                return;
            }

            if (!Owner.IsStayOnWall)
            {
                Owner.IsDirectionLocked = true;
                if (_fallDelayCount >= _fallDelayDuration)
                {
                    Owner.Fsm.ChangeState(Player.PlayerStates.Fall);
                    return;
                }
                _fallDelayCount += delta;
            }
            else _fallDelayCount = 0f;
            
            if (Owner.IsOnFloor())
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Move);
                return;
            }

            float xInput = Owner.AxisInputs().x;
            if (Mathf.Sign(Owner.WallDirection.x) == Mathf.Sign(xInput)) Owner.Velocity.x = 70 * xInput;
            else Owner.Velocity.x = 0;
            if (Owner.Velocity.y < _speedMaxY) Owner.Velocity.y += _accelerationY * delta;
        }

        public override void Exit()
        {
            Owner.IsDirectionLocked = false;
        }

        public override void Process(float delta) { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }
    }
}
