using Godot;
using AI.States;

namespace AI.Enemies
{
    public class BigHammerEnemy : Enemy
    {
        [Export]
        private IdleState _idleState = default;
        [Export]
        private ChaseState _chaseState = default;
        [Export]
        private AnimationState _animationState = default;

        public override void _Ready()
        {
            base._Ready();
            _idleState.Initialize(this, EnemyStates.Idle);
            _chaseState.Initialize(this, EnemyStates.Chase);
            _animationState.Initialize(this, EnemyStates.Attack);
            Fsm.SetCurrentState(EnemyStates.Idle);
        }

        protected override void StateController()
        {
            if (Fsm.IsStateLocked) return;

            if (Agent.TargetNavBody.IsDead || Agent.TargetNavBody.IsInactive ||
                !Agent.NavArea.IsTargetReachable)
            {
                Fsm.SetCurrentState(EnemyStates.Idle);
                return;
            }
            
            float distToTarget = Agent.DistanceToTarget();
            
            if (distToTarget < _chaseState.StopDist + 1f)
            {
                Fsm.SetCurrentState(EnemyStates.Attack, true);
                return;
            }

            if (distToTarget > _chaseState.StopDist)
            {
                Vector2 movePos =
                    Agent.TargetNavBody.NavPos - Agent.DirectionToTarget() * _chaseState.StopDist;
                if (!Agent.NavArea.IsPositionInArea(movePos)) return;
                _chaseState.TargetPos = movePos;
                Fsm.SetCurrentState(EnemyStates.Chase);
            }
        }
    }
}