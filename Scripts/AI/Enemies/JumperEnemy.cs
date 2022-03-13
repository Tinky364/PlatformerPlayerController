using Godot;
using AI.States;

namespace AI.Enemies
{
    public class JumperEnemy : Enemy
    {
        [Export]
        private IdleState _idleState = default;
        [Export]
        private ChaseState _chaseState = default;
        [Export]
        private JumpAtkState _atkState = default;

        public override void _Ready()
        {
            base._Ready();
            _idleState.Initialize(this, _idleState.State);
            _atkState.Initialize(this, _atkState.State);
            _chaseState.Initialize(this, _chaseState.State);
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
            
            if (distToTarget < _chaseState.StopDist + _chaseState.StopDistThreshold)
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
