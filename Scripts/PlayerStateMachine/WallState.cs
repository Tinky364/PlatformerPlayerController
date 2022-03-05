using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class WallState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationY = 60f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        private float _speedMaxY = 100f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _slideSpeedYNeg = 5f;
        [Export(PropertyHint.Range, "0,1,0.05,or_greater")]
        private float _fallDelayDuration = 0.25f;
        
        private Player P { get; set; }

        private float _fallDelayCount;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Wall);
            P = player;
            P.Fsm.AddState(this);
        }
        
        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            _fallDelayCount = 0f;
            P.SnapDisabled = true;
            P.PlayAnim("wall_landing");
            P.AnimPlayer.Queue("wall_slide");
            P.Velocity.x = 70f * P.WallDirection.x;
            if (P.Velocity.y > 0f) P.Velocity.y = 0f;
            else P.Velocity.y = -_slideSpeedYNeg;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.WallDirection * 2f, Vector2.Up);

            P.CastWallRay();

            if (P.IsWallJumpAble && InputManager.IsJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.WallJump);
                return;
            }
            
            if (!P.DashState.DashUnable && InputManager.IsJustPressed("dash"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Dash);
                return;
            }

            if (!P.IsStayOnWall)
            {
                P.IsDirectionLocked = true;
                if (_fallDelayCount >= _fallDelayDuration)
                {
                    P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                    return;
                }
                _fallDelayCount += delta;
            }
            else _fallDelayCount = 0f;
            
            if (P.IsOnFloor())
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                return;
            }

            float xInput = P.AxisInputs().x;
            if (Mathf.Sign(P.WallDirection.x) == Mathf.Sign(xInput)) P.Velocity.x = 70 * xInput;
            else P.Velocity.x = 0;
            if (P.Velocity.y < _speedMaxY) P.Velocity.y += _accelerationY * delta;
        }

        public override void Exit()
        {
            P.IsDirectionLocked = false;
        }
    }
}
