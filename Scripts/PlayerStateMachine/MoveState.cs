using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class MoveState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationX = 375f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _speedX = 70f;
        [Export(PropertyHint.Range, "0,10,0.05,or_greater")]
        private float _runAnimationSpeed = 1.45f;

        private float _desiredSpeedX;
        private bool IsOnPlatform => Owner.CurGroundLayer == Owner.PlatformLayer;

        public override void Initialize(Player owner, Player.PlayerStates key)
        {
            base.Initialize(owner, key);
            Owner.Fsm.AddState(this);
            Owner.PlatformCheckArea.Connect("body_exited", this, nameof(OnPlatformExited));
        }

        public override void ExitTree()
        {
            Owner.PlatformCheckArea.Disconnect("body_exited", this, nameof(OnPlatformExited));
        }
        
        public override void Enter()
        {
            GM.Print(Owner.DebugEnabled, $"{Owner.Name}: {Key}");
            Owner.DashState.SetDashSettings(true);
            Owner.SnapDisabled = true;
            if (Owner.PreVelocity.y > 50f) Owner.PlayAnimation("landing");
            Owner.Velocity.y = Owner.Gravity * Owner.GetPhysicsProcessDeltaTime();
            Owner.Velocity.x /= 2f;
        }

        public override void Process(float delta)
        {
            AnimationControl();
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            if (InputManager.IsJustPressed("jump"))
            {
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Jump);
                return;
            }
            
            if (!Owner.DashState.DashUnable && InputManager.IsJustPressed("dash"))
            {
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Dash);
                return;
            }

            if (!Owner.IsOnFloor() || FallOffPlatformInput())
            {
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
            // While the player is walking on the ground.
            _desiredSpeedX = _speedX * Owner.AxisInputs().x;
            Owner.Velocity.x = Mathf.MoveToward(
                Owner.Velocity.x, _desiredSpeedX, _accelerationX * delta
            );
            Owner.Velocity.y = Owner.Gravity * delta;
        }

        public override void Exit() { }

        private void AnimationControl()
        {
            if (Owner.AnimPlayer.CurrentAnimation.Equals("landing")) return;
            float animDuration = Owner.AnimPlayer.CurrentAnimation.Equals("run")
                ? _speedX / (Mathf.Abs(Owner.Velocity.x) * _runAnimationSpeed)
                : 2.4f;
            Owner.PlayAnimation(Mathf.Abs(Owner.Velocity.x) <=  10 ? "idle" : "run", animDuration);
        }

        private bool FallOffPlatformInput()
        {
            if (!IsOnPlatform || !InputManager.IsJustPressed("move_down")) return false;
            Owner.FallOffPlatformInput = true;
            Owner.SetCollisionMaskBit(2, false);
            Owner.GroundLayer -= Owner.PlatformLayer;
            return true;
        }

        private void OnPlatformExited(Node body)
        {
            if (!Owner.FallOffPlatformInput) return;
            Owner.FallOffPlatformInput = false;
            Owner.SetCollisionMaskBit(2, true);
            Owner.GroundLayer += Owner.PlatformLayer;
        }
    }
}
