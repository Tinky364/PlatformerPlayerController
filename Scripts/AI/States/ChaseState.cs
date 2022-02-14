using Godot;

namespace AI.States
{
    public class ChaseState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;

        [Export(PropertyHint.Range, "0,100,or_greater")]
        public float StopDist { get; private set; } = 26f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _chaseSpeed = 30f;

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

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTargetPos = _enemy.NavPos.DirectionTo(TargetPos);
            _enemy.Direction = (int) dirToTargetPos.x;
            _enemy.Velocity.x = Mathf.MoveToward(
                _enemy.Velocity.x, _enemy.Direction * _chaseSpeed, _enemy.MoveAcceleration * delta
            );
        }
        
        public override void Exit()
        {
        }
    }
}