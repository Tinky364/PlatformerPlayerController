using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class DashState : State<Player.PlayerStates>
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
        
        private Player P { get; set; }

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
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Dash);
            P = player;
            P.Fsm.AddState(this);
        }

        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            SetDashSettings(false);
            _count = 0;
            P.SnapDisabled = true;
            P.IsUnhurtable = true;
            _direction = P.AxisInputs().Clamped(1f);
            if (_direction == Vector2.Zero) _direction = P.Direction;
           
            _curDashType = FindDashType(_direction);
            SetSettingsAccordingToDashType(_curDashType);
        }

        private DashType FindDashType(Vector2 direction)
        {
            if (P.IsOnFloor()) // Dash on the ground
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
                    P.PlayAnimation("slide", _duration);
                    if (P.CollisionShape.Shape is CapsuleShape2D capsule)
                    {
                        P.CollisionShape.Position = new Vector2(0, -6);
                        capsule.Height = 2;
                    }
                    _desiredSpeed = SlideSpeed;
                    P.Velocity = _desiredSpeed * _direction +
                        Vector2.Down * P.Gravity * P.GetPhysicsProcessDeltaTime();
                    return;
                case DashType.GroundUp: 
                    P.PlayAnimation("dash_up_on_ground", _duration);
                    break;
                case DashType.GroundDown:
                    P.PlayAnimation("dash_down", _duration);
                    break;
                case DashType.GroundCrossUp:
                    P.PlayAnimation("dash_side_on_ground", _duration);
                    break;
                case DashType.AirUp: 
                    P.PlayAnimation("dash_up_in_air", _duration);
                    break;
                case DashType.AirCrossUp:
                    P.PlayAnimation("dash_side_in_air", _duration);
                    break;
                case DashType.AirCrossDown: 
                    P.PlayAnimation("dash_cross_down", _duration);
                    break;
                case DashType.AirDown:
                    P.PlayAnimation("dash_down", _duration);
                    break;
            }
            _desiredSpeed = DashSpeed;
            P.Velocity = _desiredSpeed * _direction +
                Vector2.Down * P.Gravity * P.GetPhysicsProcessDeltaTime();
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (_count > _duration)
            {
                if (P.IsOnFloor())
                {
                    P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                    return;
                }
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;

            if (_curDashType == DashType.Slide)
            {
                if (P.IsOnWall())
                {
                    P.Velocity = Vector2.Zero;
                    P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                    return;
                }
            }
            else
            {
                if (P.IsOnFloor())
                {
                    if (_curDashType == DashType.AirCrossDown)
                    {
                        P.Velocity.x = Mathf.Sign(_direction.x) * 30f;
                        P.Velocity.y = 0f;
                    }
                    else P.Velocity = Vector2.Zero;
                    P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                    return;
                }
                if (P.IsOnCeiling())
                {
                    P.Velocity = Vector2.Zero;
                    P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                    return;
                }
            }
           
            _desiredSpeed = Mathf.MoveToward(
                _desiredSpeed, _minSpeedAtTheEnd, DashAcceleration * delta
            );
            switch (_curDashType)
            {
                case DashType.Slide when !P.IsOnFloor():
                    P.Velocity.x = _desiredSpeed * _direction.x;
                    P.Velocity.y += P.Gravity * delta;
                    return;
                case DashType.Slide:
                    P.Velocity.x = _desiredSpeed * _direction.x;
                    P.Velocity.y = P.Gravity * delta;
                    return;
                default:
                    P.Velocity = _desiredSpeed * _direction;
                    return;
            }
        }

        public override void Exit()
        {
            P.IsUnhurtable = false;
            if (P.CollisionShape.Shape is CapsuleShape2D capsule)
            {
                P.CollisionShape.Position = new Vector2(0, -8);
                capsule.Height = 6;
            }
        }

        public void SetDashSettings(bool reset)
        {
            if (reset)
            {
                DashUnable = false;
                P.Sprite.SelfModulate = P.NormalSpriteColor;
            }
            else
            {
                DashUnable = true;
                P.Sprite.SelfModulate = _dashSpriteColor;
            }
        }
        
        public override void Process(float delta) { }
    }
}
