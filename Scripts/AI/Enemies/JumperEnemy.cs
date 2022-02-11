using Godot;
using AI.States;

namespace AI.Enemies
{
    public class JumperEnemy : Enemy
    {
        [Export]
        private IdleState _idleState;
        [Export]
        private ChaseState _chaseState;
        [Export]
        private JumpAttackState _attackState;

        public override void _Ready()
        {
            base._Ready();
            _idleState.Initialize(this);
            _attackState.Initialize(this);
            _chaseState.Initialize(this);
            Fsm.SetCurrentState(EnemyStates.Idle);
        }

        protected override void StateController()
        {
            if (Fsm.IsStateLocked) return;
            
            if (NavArea.TargetNavChar.IsInactive || !NavArea.IsTargetReachable)
            {
                Fsm.SetCurrentState(EnemyStates.Idle);
                return;
            }

            Vector2 dirToTarget = NavArea.DirectionToTarget();
            if (dirToTarget == Vector2.Zero) dirToTarget = Vector2.Right;
            float distToTarget = NavArea.DistanceToTarget();
            
            if (distToTarget < _chaseState.StopDist + 1f)
            {
                Fsm.SetCurrentState(EnemyStates.Attack);
                return;
            }

            if (distToTarget > _chaseState.StopDist)
            {
                Vector2 movePos = NavArea.TargetNavChar.NavPosition +
                                  -dirToTarget * _chaseState.StopDist;

                if (!NavArea.IsPositionInArea(movePos)) return;

                _chaseState.TargetPos = movePos;
                Fsm.SetCurrentState(EnemyStates.Chase);
            }
        }
    }
}
