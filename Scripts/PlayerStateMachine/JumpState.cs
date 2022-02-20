using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class JumpState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _jumpHeightMin = 10f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _jumpHeightMax = 33f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _jumpWidthMax = 40f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _jumpAccelerationX = 600f;
        
        private float JumpImpulseY => Mathf.Sqrt(2f * P.Gravity * _jumpHeightMin); // V=-sqrt{2*g*h}
        private float JumpAccelerationY => P.Gravity - Mathf.Pow(JumpImpulseY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
        private float JumpDur => JumpImpulseY / (P.Gravity - JumpAccelerationY); // t=V/(g-a)
        private float FallDur => Mathf.Sqrt(2f * _jumpHeightMax / P.Gravity); // t=sqrt{(2*h)/g}
        public float JumpSpeedX => _jumpWidthMax / (JumpDur + FallDur); // v=w/t
        
        private float _desiredJumpSpeedX;
        private bool _isFirstFrame;

        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.AnimPlayer.GetAnimation("jump").Length = JumpDur;
            P.AnimPlayer.Play("jump");
            P.JumpTimer.Start(JumpDur);
            _isFirstFrame = true;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            _desiredJumpSpeedX = JumpSpeedX * P.AxisInputs().x;
            if (_isFirstFrame)
            {
                _isFirstFrame = false;
                P.Velocity.x = _desiredJumpSpeedX;
                P.Velocity.y = -JumpImpulseY;
                return;
            }
            if (Input.IsActionPressed("jump") && P.Velocity.y <= 0f)
            {
                P.Velocity.x = Mathf.MoveToward(P.Velocity.x, _desiredJumpSpeedX, _jumpAccelerationX * delta);
                P.Velocity.y += (P.Gravity - JumpAccelerationY) * delta;
                return;
            }
            
            OnJumpEnd();
        }

        private void OnJumpEnd()
        {
            P.JumpTimer.Stop();
            P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }

        public override void Exit() { }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Jump);
            P = player;
            P.Fsm.AddState(this);
            P.AnimPlayer.GetAnimation("fall").Length = FallDur;
        }
    }
}