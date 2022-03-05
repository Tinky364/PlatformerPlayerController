using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class MoveState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationX = 375f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _speedX = 70f;
        [Export(PropertyHint.Range, "0,10,0.05,or_greater")]
        private float _runAnimationSpeed = 1.45f;

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
            P.DashState.SetDashSettings(true);
            P.SnapDisabled = true;
            if (P.PreVelocity.y > 50f) P.PlayAnim("landing");
            P.Velocity.y = P.Gravity * P.GetPhysicsProcessDeltaTime();
            P.Velocity.x /= 2f;
        }

        public override void Process(float delta)
        {
            AnimationControl();
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (InputManager.IsJustPressed("jump"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }
            
            if (!P.DashState.DashUnable && InputManager.IsJustPressed("dash"))
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Dash);
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

        public override void Exit() { }
        
        private void AnimationControl()
        {
            if (P.AnimPlayer.CurrentAnimation.Equals("landing")) return;
            float animDuration = P.AnimPlayer.CurrentAnimation.Equals("run")
                ? _speedX / (Mathf.Abs(P.Velocity.x) * _runAnimationSpeed)
                : 2.4f;
            P.PlayAnim(Mathf.Abs(P.Velocity.x) <=  10 ? "idle" : "run", animDuration);
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
