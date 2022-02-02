using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class IdleState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;

        private Vector2 _pos1 = new Vector2();
        private Vector2 _pos2 = new Vector2();
        private Vector2 _targetPos = new Vector2();

        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Idle);
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
            
            _pos1 = _enemy.NavChar.GlobalPosition;
            _pos2 = _pos1 + new Vector2(30f, 0f);
            if (!_enemy.NavArea.IsPositionInArea(_pos2))
            {
                _pos2 = _pos1 - new Vector2(30f, 0f);
                if (!_enemy.NavArea.IsPositionInArea(_pos2))
                {
                    GD.PrintErr("Not enough space for the enemy idle motion!");
                    return;
                }
            }
            _targetPos = _pos2;
        }
        
        public override void Enter()
        {
            GD.Print($"{_enemy.Name}: IdleState");
            _enemy.AnimatedSprite.Play("idle");
        }

        public override void Exit()
        {
        }

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTarget = _targetPos - _enemy.NavChar.NavPosition;
            if (Mathf.Abs(dirToTarget.x) > 1f)
            {
                _enemy.AnimatedSprite.Play("run");
                _enemy.Velocity.x = dirToTarget.Normalized().x * _enemy.MoveSpeed;
            }
            else
            {
                _targetPos = _targetPos == _pos2 ? _pos1 : _pos2;
            }
        }
    }
}