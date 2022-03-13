using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class DashState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _airDashLength = 48f;
        [Export(PropertyHint.Range, "1,100,or_greater")]
        private float _slideLength = 42f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _duration = 0.275f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _minSpeedAtTheEnd = 60f;
        [Export]
        private Color _dashSpriteColor;
        
        public bool DashUnable { get; private set; }
        private Vector2 _direction;
        private float DashAcceleration =>
            2f * (_airDashLength - _minSpeedAtTheEnd * _duration) / Mathf.Pow(_duration, 2);
        private float DashSpeed => _minSpeedAtTheEnd + DashAcceleration * _duration;
        private float SlideAcceleration =>
            2f * (_slideLength - _minSpeedAtTheEnd * _duration) / Mathf.Pow(_duration, 2);
        private float SlideSpeed => _minSpeedAtTheEnd + SlideAcceleration * _duration;
        private float _count;
        private float _desiredSpeed;

        private enum DashType
        {
            Slide, GroundUp, GroundDown,
            GroundCrossUp, AirUp, AirDown,
            AirCrossUp, AirCrossDown
        }
        private DashType _curDashType;
        
        public override void Enter()
        {
            GM.Print(Owner.DebugEnabled, $"{Owner.Name}: {Key}");
            SetDashSettings(false);
            _count = 0;
            Owner.SnapDisabled = true;
            Owner.IsUnhurtable = true;
            _direction = Owner.AxisInputs().Clamped(1f);
            if (_direction == Vector2.Zero) _direction = Owner.Direction;
           
            _curDashType = FindDashType(_direction);
            SetSettingsAccordingToDashType(_curDashType);
        }

        private DashType FindDashType(Vector2 direction)
        {
            if (Owner.IsOnFloor()) // Dash on the ground
            {
                if (direction.y == 1f) return DashType.GroundDown;
                if (direction.y == -1f) return DashType.GroundUp;
                if (direction.y >= 0f) return DashType.Slide;
                if (direction.y < 0f) return DashType.GroundCrossUp;
            }
            else // Dash in the air
            {
                if (direction.y == -1f) return DashType.AirUp;
                if (direction.y == 1f) return DashType.AirDown;
                if (direction.y <= 0f) return DashType.AirCrossUp;
                if (direction.y > 0f) return DashType.AirCrossDown;
            }
            return DashType.GroundDown;
        }

        private void SetSettingsAccordingToDashType(DashType type)
        {
            switch (type)
            {
                case DashType.Slide:
                    _direction = new Vector2(Mathf.Sign(_direction.x), 0);
                    Owner.PlayAnimation("slide", _duration);
                    if (Owner.CollisionShape.Shape is CapsuleShape2D capsule)
                    {
                        Owner.CollisionShape.Position = new Vector2(0, -6);
                        capsule.Height = 2;
                    }
                    _desiredSpeed = SlideSpeed;
                    Owner.Velocity = _desiredSpeed * _direction +
                        Vector2.Down * Owner.Gravity * Owner.GetPhysicsProcessDeltaTime();
                    return;
                case DashType.GroundUp: 
                    Owner.PlayAnimation("dash_up_on_ground", _duration);
                    break;
                case DashType.GroundDown:
                    Owner.PlayAnimation("dash_down", _duration);
                    break;
                case DashType.GroundCrossUp:
                    Owner.PlayAnimation("dash_side_on_ground", _duration);
                    break;
                case DashType.AirUp: 
                    Owner.PlayAnimation("dash_up_in_air", _duration);
                    break;
                case DashType.AirCrossUp:
                    Owner.PlayAnimation("dash_side_in_air", _duration);
                    break;
                case DashType.AirCrossDown: 
                    Owner.PlayAnimation("dash_cross_down", _duration);
                    break;
                case DashType.AirDown:
                    Owner.PlayAnimation("dash_down", _duration);
                    break;
            }
            _desiredSpeed = DashSpeed;
            Owner.Velocity = _desiredSpeed * _direction +
                Vector2.Down * Owner.Gravity * Owner.GetPhysicsProcessDeltaTime();
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            if (_count > _duration)
            {
                if (Owner.IsOnFloor())
                {
                    Owner.Fsm.SetCurrentState(Player.PlayerStates.Move);
                    return;
                }
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;

            if (_curDashType == DashType.Slide)
            {
                if (Owner.IsOnWall())
                {
                    Owner.Velocity = Vector2.Zero;
                    Owner.Fsm.SetCurrentState(Player.PlayerStates.Move);
                    return;
                }
            }
            else
            {
                if (Owner.IsOnFloor())
                {
                    if (_curDashType == DashType.AirCrossDown)
                    {
                        Owner.Velocity.x = Mathf.Sign(_direction.x) * 30f;
                        Owner.Velocity.y = 0f;
                    }
                    else Owner.Velocity = Vector2.Zero;
                    Owner.Fsm.SetCurrentState(Player.PlayerStates.Move);
                    return;
                }
                if (Owner.IsOnCeiling())
                {
                    Owner.Velocity = Vector2.Zero;
                    Owner.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                    return;
                }
            }
           
            _desiredSpeed = Mathf.MoveToward(
                _desiredSpeed, _minSpeedAtTheEnd, DashAcceleration * delta
            );
            switch (_curDashType)
            {
                case DashType.Slide when !Owner.IsOnFloor():
                    Owner.Velocity.x = _desiredSpeed * _direction.x;
                    Owner.Velocity.y += Owner.Gravity * delta;
                    return;
                case DashType.Slide:
                    Owner.Velocity.x = _desiredSpeed * _direction.x;
                    Owner.Velocity.y = Owner.Gravity * delta;
                    return;
                default:
                    Owner.Velocity = _desiredSpeed * _direction;
                    return;
            }
        }

        public override void Exit()
        {
            Owner.IsUnhurtable = false;
            if (Owner.CollisionShape.Shape is CapsuleShape2D capsule)
            {
                Owner.CollisionShape.Position = new Vector2(0, -8);
                capsule.Height = 6;
            }
        }

        public override void ExitTree() { }

        public void SetDashSettings(bool reset)
        {
            if (reset)
            {
                DashUnable = false;
                Owner.Sprite.SelfModulate = Owner.NormalSpriteColor;
            }
            else
            {
                DashUnable = true;
                Owner.Sprite.SelfModulate = _dashSpriteColor;
            }
        }
        
        public override void Process(float delta) { }
    }
}
