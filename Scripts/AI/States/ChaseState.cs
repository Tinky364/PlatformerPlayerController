using Godot;
using Manager;

namespace AI.States
{
    public class ChaseState : State<Enemy.EnemyStates>
    {
        [Export(PropertyHint.Range, "0,100,or_greater")]
        public float StopDist { get; private set; } = 26f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _chaseSpeed = 30f;

        private Enemy E { get; set; }

        public Vector2 TargetPos { get; set; }
        
        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Chase);
            E = enemy;
            E.Fsm.AddState(this);
        }

        public override void Enter()
        {
            GM.Print(E.Agent.DebugEnabled, $"{E.Name}: {Key}");
            E.Agent.Velocity.x = 0f;
            E.AnimatedSprite.Play("run");
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTargetPos = E.Agent.NavPos.DirectionTo(TargetPos);
            E.Agent.Direction.x = dirToTargetPos.x;
            E.Agent.Velocity.x = Mathf.MoveToward(
                E.Agent.Velocity.x, E.Agent.Direction.x * _chaseSpeed, E.MoveAcceleration * delta
            );
        }
        
        public override void Process(float delta) { }

        public override void Exit() { }
    }
}