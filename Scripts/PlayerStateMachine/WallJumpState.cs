using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class WallJumpState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _heightMin = 22f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _heightMax = 26f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _widthMax = 50f;
        
        private float SpeedX => _widthMax / (JumpDur + FallDur); // v=w/t
        private float ImpulseY => Mathf.Sqrt(2f * Owner.Gravity * _heightMin); // V=-sqrt{2*g*h}
        private float AccelerationY =>
            Owner.Gravity - Mathf.Pow(ImpulseY, 2) / (2 * _heightMax); // a=g-(v^2/2*h)
        private float JumpDur => ImpulseY / (Owner.Gravity - AccelerationY); // t=V/(g-a)
        private float FallDur => Mathf.Sqrt(2f * _heightMax / Owner.Gravity); // t=sqrt{(2*h)/g}
        private float _desiredSpeedX;
        private float _count;

        public override void Initialize(Player owner, Player.PlayerStates key)
        {
            base.Initialize(owner, key);
            Owner.Fsm.AddState(this);
        }

        public override void Enter()
        {
            GM.Print(Owner.DebugEnabled, $"{Owner.Name}: {Key}");
            _count = 0;
            Owner.SnapDisabled = true;
            Owner.PlayAnimation("wall_jump", JumpDur);
            Owner.Direction.x = -Owner.WallDirection.x;
            Owner.Velocity.x = SpeedX * -Owner.WallDirection.x;
            Owner.Velocity.y = -ImpulseY;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            Owner.CastWallRay();

            if (_count > JumpDur)
            {
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;
            
            if (Owner.IsStayOnWall)
            {
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Wall);
                return;
            }
            
            if (!Owner.DashState.DashUnable && InputManager.IsJustPressed("dash"))
            {
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Dash);
                return;
            }
            
            if (InputManager.IsPressed("jump") && Owner.Velocity.y <= 0f)
            {
                _desiredSpeedX = (SpeedX - 10f) * Owner.AxisInputs().x;
                Owner.Velocity.x = Mathf.MoveToward(
                    Owner.Velocity.x, _desiredSpeedX, Owner.AirAccelerationX * delta
                );
                Owner.Velocity.y += (Owner.Gravity - AccelerationY) * delta;
                return;
            }
            
            // Starts fall when there is no jump input.
            Owner.Fsm.SetCurrentState(Player.PlayerStates.Fall);
        }

        public override void Exit() { }
        
        public override void ExitTree() { }
    }
}