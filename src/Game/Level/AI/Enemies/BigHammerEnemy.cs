using Godot;
using Game.Level.AI.States;

namespace Game.Level.AI.Enemies
{
    public class BigHammerEnemy : Enemy
    {
        [Export]
        private AiStateIdle _aiStateIdle = default;
        [Export]
        private AiStateChase _aiStateChase = default;
        [Export]
        private AiStateAnimation _aiStateAnimation = default;

        public new BigHammerEnemy Init()
        {
            base.Init();
            _aiStateIdle.Init(this);
            _aiStateChase.Init(this);
            _aiStateAnimation.Init(this);
            Fsm.ChangeState(EnemyStates.Idle);
            return this;
        }

        protected override void StateController()
        {
            if (Fsm.IsStateLocked) return;

            if (Agent.TargetNavBody.IsInactive || !Agent.NavArea.IsTargetReachable)
            {
                Fsm.ChangeState(EnemyStates.Idle);
                return;
            }
            
            float distToTarget = Agent.DistanceToTarget();
            
            if (distToTarget < _aiStateChase.StopDist + 1f)
            {
                Fsm.ChangeState(EnemyStates.Attack, true);
                return;
            }

            if (distToTarget > _aiStateChase.StopDist)
            {
                Vector2 movePos = Agent.TargetNavBody.NavPos - Agent.DirectionToTarget() * _aiStateChase.StopDist;
                if (!Agent.NavArea.IsPositionInArea(movePos)) return;
                _aiStateChase.TargetPos = movePos;
                Fsm.ChangeState(EnemyStates.Chase);
            }
        }
    }
}