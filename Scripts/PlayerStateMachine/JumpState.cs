using Godot;
using AI;

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
        private float JumpSpeedX => _jumpWidthMax / (JumpDur + FallDur); // v=w/t
        
        private float _desiredJumpSpeedX;

        private bool _isFirstFrame;

        public override void Enter()
        {
            GD.Print($"{P.Name}: {Key}");
            P.JumpTimer.Start(JumpDur);
            _isFirstFrame = true;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.AxisInputs();
            _desiredJumpSpeedX = JumpSpeedX * P.InputAxis.x;

            if (_isFirstFrame)
            {
                P.Velocity.x = _desiredJumpSpeedX;
                P.Velocity.y = -JumpImpulseY;
            }
            else
            {
                if (Input.IsActionPressed("jump"))
                {
                    P.Velocity.x = Mathf.MoveToward(P.Velocity.x, _desiredJumpSpeedX, _jumpAccelerationX * delta);
                    if (P.Velocity.y > 0f)
                    {
                        OnJumpEnd();
                        return;
                    }
                    P.Velocity.y += (P.Gravity - JumpAccelerationY) * delta;
                }
                else
                {
                    OnJumpEnd();
                    return;
                }
            }            
           
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, Vector2.Zero, Vector2.Up);
            _isFirstFrame = false;
        }

        public override void Exit()
        {
            _desiredJumpSpeedX = 0;
        }

        public void OnJumpEnd()
        {
            GD.Print("stopped");
            P.JumpTimer.Stop();
            P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Jump);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}