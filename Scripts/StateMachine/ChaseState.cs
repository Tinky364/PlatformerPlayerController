using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class ChaseState : State<Enemy.EnemyStates>
    {
        private readonly Enemy _enemy;

        protected ChaseState() {}

        public ChaseState(Enemy enemy) : base(Enemy.EnemyStates.Chase)
        {
            _enemy = enemy;
        }
        
        public override void Enter()
        {
            GD.Print("ChaseState");
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
            if (_enemy.IsOnGround && _enemy.NavArea.IsTargetReachable)
            {
                Vector2 dirToTarget = _enemy.NavArea.TargetNavBody.NavPosition - _enemy.NavBody.NavPosition;
                if (Mathf.Abs(dirToTarget.x) > _enemy.StopDistance)
                {
                    _enemy.Velocity.x = dirToTarget.Normalized().x * _enemy.MoveSpeed;
                }
                else
                {
                    _enemy.Velocity.x = 0f;
                    _enemy.Machine.SetCurrentState(Enemy.EnemyStates.Attack);
                }
            }
            else
            {
                _enemy.Velocity.x = 0f;
            }
        }
    }
}