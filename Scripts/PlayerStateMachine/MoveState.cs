using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class MoveState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationX = 400f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _speedX = 60f;

        private Player P { get; set; }

        private float _desiredSpeedX;
        private bool IsOnPlatform => P.CurGroundLayer == P.PlatformLayer;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Move);
            P = player;
            P.Fsm.AddState(this);
        }
        
        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.AnimPlayer.GetAnimation("hit_ground").Length = 0.075f;
            P.AnimPlayer.Play("hit_ground");
            P.Velocity.y = P.Gravity * P.GetPhysicsProcessDeltaTime();
        }

        public override void Process(float delta)
        {
            if (!P.AnimPlayer.CurrentAnimation.Equals("hit_ground"))
            {
                P.AnimPlayer.Play(P.Velocity.x == 0 ? "idle" : "run");
                P.AnimPlayer.PlaybackSpeed = P.AnimPlayer.CurrentAnimation.Equals("run")
                    ? Mathf.Clamp(Mathf.Abs(P.Velocity.x) / _speedX, 0.5f, 1f)
                    : 1f;
            }
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (InputManager.IsJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }

            if (!P.IsOnFloor() || FallOffPlatformInput())
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
            // While the player is walking on the ground.
            _desiredSpeedX = _speedX * P.AxisInputs().x;
            P.Velocity.x = Mathf.MoveToward(
                P.Velocity.x, _desiredSpeedX, _accelerationX * delta
            );
            P.Velocity.y = P.Gravity * delta;
        }

        public override void Exit()
        {
            P.AnimPlayer.PlaybackSpeed = 1f;
        }

        private bool FallOffPlatformInput()
        {
            if (!IsOnPlatform || !InputManager.IsJustPressed("move_down")) return false;
            P.FallOffPlatformInput = true;
            P.SetCollisionMaskBit(2, false);
            P.GroundLayer -= P.PlatformLayer;
            return true;
        }

        private void OnPlatformExited(Node body)
        {
            if (!P.FallOffPlatformInput) return;
            P.FallOffPlatformInput = false;
            P.SetCollisionMaskBit(2, true);
            P.GroundLayer += P.PlatformLayer;
        }
    }
}
