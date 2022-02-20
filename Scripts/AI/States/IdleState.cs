using Godot;
using Manager;

namespace AI.States
{
    public class IdleState : State<Enemy.EnemyStates>
    {
        private Enemy E { get; set; }

        [Export]
        private float _secondPosDist = 40f;

        private Vector2 _pos1 = new Vector2();
        private Vector2 _pos2 = new Vector2();
        private Vector2 _targetPos = new Vector2();

        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Idle);
            E = enemy;
            E.Fsm.AddState(this);
            
            _pos1 = E.Agent.NavPos;
            _pos2 = _pos1 + new Vector2(_secondPosDist, 0f);
            if (!E.Agent.NavArea.IsPositionInArea(_pos2))
            {
                _pos2 = _pos1 - new Vector2(_secondPosDist, 0f);
                if (!E.Agent.NavArea.IsPositionInArea(_pos2))
                {
                    GD.PrintErr("Not enough space for the enemy idle motion!");
                    return;
                }
            }
            _targetPos = _pos2;
        }
        
        public override void Enter()
        {
            GM.Print(E.Agent.DebugEnabled, $"{E.Name}: {Key}");
            E.AnimatedSprite.Play("idle");
        }

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTarget = _targetPos - E.Agent.NavPos;
            if (Mathf.Abs(dirToTarget.x) > 1f)
            {
                E.AnimatedSprite.Play("run");
                E.Agent.Direction.x = dirToTarget.Normalized().x;
                E.Agent.Velocity.x = Mathf.MoveToward(
                    E.Agent.Velocity.x, E.Agent.Direction.x * E.MoveSpeed,
                    E.MoveAcceleration * delta
                );
            }
            else
            {
                _targetPos = _targetPos == _pos2 ? _pos1 : _pos2;
            }
        }

        public override void Exit()
        {
        }
    }
}