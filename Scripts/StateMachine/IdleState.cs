using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class IdleState : State<Enemy.EnemyStates>
    {
        private readonly Enemy _enemy;

        private Vector2 _pos1 = new Vector2();
        private Vector2 _pos2 = new Vector2();
        private Vector2 _targetPos = new Vector2();

        protected IdleState() {}

        public IdleState(Enemy enemy) : base(Enemy.EnemyStates.Idle)
        {
            _enemy = enemy;
            _pos1 = _enemy.Body.GlobalPosition;
            _pos2 = _pos1 + new Vector2(50f, 0f);
            if (!_enemy.NavArea.IsPositionInArea(_pos2))
                _pos2 = _pos1 - new Vector2(50f, 0f);
            _targetPos = _pos2;
        }
        
        public override void Enter()
        {
            GD.Print("IdleState");
        }

        public override void Exit()
        {
        }

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
            ActionLogic(delta);
        }

        private void ActionLogic(float delta)
        {
            if (_enemy.IsOnGround)
            {
                Vector2 dirToTarget = _targetPos - _enemy.NavBody.NavPosition;
                if (Mathf.Abs(dirToTarget.x) > 1f)
                {
                    _enemy.Velocity.x = dirToTarget.Normalized().x * _enemy.MoveSpeed;
                }
                else
                {
                    _targetPos = _targetPos == _pos2 ? _pos1 : _pos2;
                }
            }
            else
            {
                _enemy.Velocity.x = 0f;
            }
        }
    }
}