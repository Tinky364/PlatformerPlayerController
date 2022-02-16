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
            if (_enemy.Body.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(ChaseState)}");
            _enemy.Body.Velocity.x = 0f;
            _enemy.AnimatedSprite.Play("run");
        }

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTargetPos = _enemy.Body.NavPos.DirectionTo(TargetPos);
            _enemy.Body.Direction = (int) dirToTargetPos.x;
            _enemy.Body.Velocity.x = Mathf.MoveToward(
                _enemy.Body.Velocity.x, _enemy.Body.Direction * _chaseSpeed, _enemy.MoveAcceleration * delta
            );
        }
        
        public override void Exit()
        {
        }
    }
}