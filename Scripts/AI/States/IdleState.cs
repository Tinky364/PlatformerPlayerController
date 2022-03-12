using Godot;
using Manager;

namespace AI.States
{
    public class IdleState : State<Enemy.EnemyStates>
    {
        [Export]
        private Enemy.EnemyStates State { get; set; } = Enemy.EnemyStates.Idle;
        [Export]
        private IdleType _idleType = IdleType.Stay;
        [Export]
        private float _secondPosDist = 40f;
        [Export]
        private float _stayDur = 2f;

        private enum IdleType { Stay, Move }
        private Enemy E { get; set; }
        private Vector2 _pos1 = new Vector2();
        private Vector2 _pos2 = new Vector2();
        private Vector2 _targetPos = new Vector2();
        private float _stayCount = 0;

        public void Initialize(Enemy enemy)
        {
            Initialize(State);
            E = enemy;
            E.Fsm.AddState(this);
            
            _pos1 = E.Agent.NavPos;
            switch (_idleType)
            {
                case IdleType.Stay:
                    _targetPos = _pos1;
                    break;
                case IdleType.Move: 
                    SetMovePosition();
                    _targetPos = _pos2;
                    break;
            }
        }
        
        public override void Enter()
        {
            GM.Print(E.Agent.DebugEnabled, $"{E.Name}: {Key}");
        }

        public override void PhysicsProcess(float delta)
        {
            switch (_idleType)
            {
                case IdleType.Stay: StayLoop(delta);
                    break;
                case IdleType.Move: MoveLoop(delta);
                    break;
            }
        }

        public override void Process(float delta) { }

        public override void Exit() { }

        private void SetMovePosition()
        {
            _pos2 = _pos1 + new Vector2(_secondPosDist, 0f);
            if (!E.Agent.NavArea.IsPositionInArea(_pos2))
                GD.PrintErr("Not enough space for the enemy idle motion!");
        }

        private void MoveLoop(float delta)
        {
            Vector2 distToTarget = _targetPos - E.Agent.NavPos;
            if (Mathf.Abs(distToTarget.x) > 1f) // Moves to the target position.
            {
                E.PlayAnimation("run");
                E.Agent.Direction.x = distToTarget.Normalized().x; // Direction to target.
                E.Agent.Velocity.x = Mathf.MoveToward(
                    E.Agent.Velocity.x, E.Agent.Direction.x * E.MoveSpeed, 
                    E.MoveAcceleration * delta
                );
            }
            else // When it is in the target position.
            {
                E.Agent.Velocity.x = 0;
                if (_stayCount < _stayDur) // Stays in idle position until duration expired.
                {
                    E.PlayAnimation("idle");
                    _stayCount += delta;
                }
                else // New target position is declared. 
                {
                    _targetPos = _targetPos == _pos2 ? _pos1 : _pos2;
                    _stayCount = 0;
                }
            }
        }

        private void StayLoop(float delta)
        {
            Vector2 distToTarget = _targetPos - E.Agent.NavPos;
            if (Mathf.Abs(distToTarget.x) > 1f) // Moves to the target position.
            {
                E.PlayAnimation("run");
                E.Agent.Direction.x = distToTarget.Normalized().x; // Direction to target.
                E.Agent.Velocity.x = Mathf.MoveToward(
                    E.Agent.Velocity.x, E.Agent.Direction.x * E.MoveSpeed, 
                    E.MoveAcceleration * delta
                );
            }
            else
            {
                E.Agent.Velocity.x = 0;
                E.PlayAnimation("idle");
            }
        }
    }
}