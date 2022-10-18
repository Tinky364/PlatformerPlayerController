using CustomRegister;
using Godot;
using Game.Fsm;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.Players.States
{
    [Register]
    public class PlayerStateJump : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _heightMin = 16f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _heightMax = 32f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _widthMax = 48f;

        public float SpeedX => _widthMax / (JumpDur + FallDur); // v=w/t
        private float ImpulseY => Mathf.Sqrt(2f * Owner.Gravity * _heightMin); // V=-sqrt{2*g*h}
        private float AccelerationY =>
            Owner.Gravity - Mathf.Pow(ImpulseY, 2) / (2 * _heightMax); // a=g-(v^2/2*h)
        private float JumpDur => ImpulseY / (Owner.Gravity - AccelerationY); // t=V/(g-a)
        private float FallDur => Mathf.Sqrt(2f * _heightMax / Owner.Gravity); // t=sqrt{(2*h)/g}
        private float _desiredSpeedX;
        private float _count;

        public override void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            _count = 0;
            Owner.SnapDisabled = true;
            Owner.PlayAnimation(Mathf.Abs(Owner.Velocity.x) > 30f ? "jump_side" : "jump_up", JumpDur);
            _desiredSpeedX = SpeedX * Owner.AxisInputs().x;
            Owner.Velocity.x = _desiredSpeedX;
            Owner.Velocity.y = -ImpulseY;
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            Owner.CastWallRay();

            if (_count > JumpDur)
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;
            
            if (Owner.IsStayOnWall)
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Wall);
                return;
            }

            if (!Owner.PlayerStateDash.DashUnable && InputInvoker.IsPressed("dash"))
            {
                Owner.Fsm.ChangeState(Player.PlayerStates.Dash);
                return;
            }
            
            if (InputInvoker.IsPressing("jump") && Owner.Velocity.y <= 0f)
            {
                _desiredSpeedX = SpeedX * Owner.AxisInputs().x;
                Owner.Velocity.x = Mathf.MoveToward(
                    Owner.Velocity.x, _desiredSpeedX, Owner.AirAccelerationX * delta
                );
                Owner.Velocity.y += (Owner.Gravity - AccelerationY) * delta;
                return;
            }
            
            // Starts fall when there is no jump input.
            Owner.Fsm.ChangeState(Player.PlayerStates.Fall);
        }

        public override void Process(float delta) { }
        public override void Exit() { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }
    }
}