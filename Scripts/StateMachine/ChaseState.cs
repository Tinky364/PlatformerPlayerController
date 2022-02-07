using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class ChaseState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;

        [Export(PropertyHint.Range, "0,100,or_greater")]
        public float StopDist = 26f;

        public Vector2 TargetPos;
        
        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Chase);
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
        }

        public override void Enter()
        {
            if (_enemy.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(ChaseState)}");
            _enemy.Velocity.x = 0f;
            _enemy.AnimatedSprite.Play("run");
        }

        public override void Exit()
        {
        }

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTargetPos = _enemy.NavChar.NavPosition.DirectionTo(TargetPos);
            _enemy.Velocity.x = dirToTargetPos.x * _enemy.MoveSpeed;
        }
    }
}