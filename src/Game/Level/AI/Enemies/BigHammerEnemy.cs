using Godot;
using Game.Level.AI.States;

namespace Game.Level.AI.Enemies
{
    public class BigHammerEnemy : Enemy
    {
        [Export]
        private IdleState _idleState = default;
        [Export]
        private ChaseState _chaseState = default;
        [Export]
        private AnimationState _animationState = default;

        public new BigHammerEnemy Init()
        {
            base.Init();
            _idleState.Init(this);
            _chaseState.Init(this);
            _animationState.Init(this);
            Fsm.ChangeState(EnemyStates.Idle);
            return this;
        }

        protected override void StateController()
        {
            if (Fsm.IsStateLocked) return;

            if (Agent.TargetNavBody.IsDead || Agent.TargetNavBody.IsInactive ||
                !Agent.NavArea.IsTargetReachable)
            {
                Fsm.ChangeState(EnemyStates.Idle);
                return;
            }
            
            float distToTarget = Agent.DistanceToTarget();
            
            if (distToTarget < _chaseState.StopDist + 1f)
            {
                Fsm.ChangeState(EnemyStates.Attack, true);
                return;
            }

            if (distToTarget > _chaseState.StopDist)
            {
                Vector2 movePos =
                    Agent.TargetNavBody.NavPos - Agent.DirectionToTarget() * _chaseState.StopDist;
                if (!Agent.NavArea.IsPositionInArea(movePos)) return;
                _chaseState.TargetPos = movePos;
                Fsm.ChangeState(EnemyStates.Chase);
            }
        }
    }
}