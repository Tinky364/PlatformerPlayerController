using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class WallJumpState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _heightMin = 10f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _heightMax = 20f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _widthMax = 40f;
        
        private Player P { get; set; }
        
        public float SpeedX => _widthMax / (JumpDur + FallDur); // v=w/t
        private float ImpulseY => Mathf.Sqrt(2f * P.Gravity * _heightMin); // V=-sqrt{2*g*h}
        private float AccelerationY =>
            P.Gravity - Mathf.Pow(ImpulseY, 2) / (2 * _heightMax); // a=g-(v^2/2*h)
        private float JumpDur => ImpulseY / (P.Gravity - AccelerationY); // t=V/(g-a)
        private float FallDur => Mathf.Sqrt(2f * _heightMax / P.Gravity); // t=sqrt{(2*h)/g}
        private float _desiredSpeedX;
        private float _count;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.WallJump);
            P = player;
            P.Fsm.AddState(this);
        }

        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            _count = 0;
            P.SnapDisabled = true;
            P.AnimPlayer.GetAnimation("wall_jump").Length = JumpDur;
            P.AnimPlayer.Play("wall_jump");
            P.Velocity.x = SpeedX * -P.WallDirection.x;
            P.Velocity.y = -ImpulseY;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            P.CastWallRay();

            if (_count > JumpDur)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;
            
            if (P.IsWallRayHit && P.IsOnWall)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Wall);
                return;
            }
            
            if (InputManager.IsPressed("jump") && P.Velocity.y <= 0f)
            {
                _desiredSpeedX = (SpeedX - 10f) * P.AxisInputs().x;
                P.Velocity.x = Mathf.MoveToward(
                    P.Velocity.x, _desiredSpeedX, P.AirAccelerationX * delta
                );
                P.Velocity.y += (P.Gravity - AccelerationY) * delta;
                return;
            }
            
            // Starts fall when there is no jump input.
            P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }

        public override void Exit() { }
    }
}