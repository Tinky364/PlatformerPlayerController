using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class DashState : State<Player.PlayerStates>
    {
        [Export]
        private float _length = 32f;
        [Export]
        private float _duration = 0.225f;
        [Export]
        private float _minSpeedAtTheEnd = 40f;
        
        private Player P { get; set; }

        public bool DashUnable { get; private set; }
        private Vector2 _direction;
        private float Acceleration =>
            2f * (_length - _minSpeedAtTheEnd * _duration) / Mathf.Pow(_duration, 2);
        private float Speed => _minSpeedAtTheEnd + Acceleration * _duration;
        private float _count;
        private float _desiredSpeed;

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
            P.AnimPlayer.Play("jump");
            _direction = P.AxisInputs().Clamped(1f);
            if (_direction == Vector2.Zero) _direction = P.Direction;
            _desiredSpeed = Speed;
            P.Velocity = _desiredSpeed * _direction;
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (_count > _duration)
            {
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;

            if (P.GetLastSlideCollision() is KinematicCollision2D collision)
            {
                if (Mathf.Abs(collision.Normal.y) > 0.5f)
                    _direction = _direction.Bounce(collision.Normal);
            }

            _desiredSpeed = Mathf.MoveToward(
                _desiredSpeed, _minSpeedAtTheEnd, Acceleration * delta
            );
            P.Velocity = _desiredSpeed * _direction;
        }

        public override void Exit()
        {
            P.IsUnhurtable = false;
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
                P.Sprite.SelfModulate = P.DashSpriteColor;
            }
        }
        
        public override void Process(float delta) { }
    }
}
