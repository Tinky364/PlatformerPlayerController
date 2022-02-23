using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class WallState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationY = 100f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        private float _speedMaxY = 100f;
        
        private Player P { get; set; }

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Wall);
            P = player;
            P.Fsm.AddState(this);
        }
        
        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.AnimPlayer.Play("wall");
            P.Velocity.x = 70 * P.WallDirection.x;
            if (P.Velocity.y > 0f) P.Velocity.y = 0f;
            else P.Velocity.y /= 4f;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.WallDirection * 2f, Vector2.Up);

            P.CastWallRay();

            if (P.IsWallRayHit && InputManager.IsJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.WallJump);
                return;
            }

            if (!P.IsOnWall)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
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

        public override void Exit() { }
    }
}
