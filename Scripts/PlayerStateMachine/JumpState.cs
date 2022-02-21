using Godot;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class JumpState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _jumpHeightMin = 10f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _jumpHeightMax = 33f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _jumpWidthMax = 40f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _jumpAccelerationX = 600f;

        private Player P { get; set; }

        public float JumpSpeedX => _jumpWidthMax / (JumpDur + FallDur); // v=w/t
        private float JumpImpulseY => Mathf.Sqrt(2f * P.Gravity * _jumpHeightMin); // V=-sqrt{2*g*h}
        private float JumpAccelerationY =>
            P.Gravity - Mathf.Pow(JumpImpulseY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
        private float JumpDur => JumpImpulseY / (P.Gravity - JumpAccelerationY); // t=V/(g-a)
        private float FallDur => Mathf.Sqrt(2f * _jumpHeightMax / P.Gravity); // t=sqrt{(2*h)/g}
        private float _desiredJumpSpeedX;
        private bool _isFirstFrame;
        private float _count;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Jump);
            P = player;
            P.Fsm.AddState(this);
            P.AnimPlayer.GetAnimation("fall").Length = FallDur;
        }
        
        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            _count = 0;
            P.SnapDisabled = true;
            P.AnimPlayer.GetAnimation("jump").Length = JumpDur;
            P.AnimPlayer.Play("jump");
            _isFirstFrame = true;
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (_count > JumpDur)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;

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
                P.Velocity.x = Mathf.MoveToward(
                    P.Velocity.x, _desiredJumpSpeedX, _jumpAccelerationX * delta
                );
                P.Velocity.y += (P.Gravity - JumpAccelerationY) * delta;
                return;
            }
            
            // Starts fall when there is no jump input.
            P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }

        public override void Process(float delta) { }
        
        public override void Exit() { }
    }
}