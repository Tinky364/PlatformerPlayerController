using Godot;
using Game.Level.AI.States;

namespace Game.Level.AI.Enemies
{
    public class RusherEnemy : Enemy
    {
        [Export]
        private IdleState _idleState = default;
        [Export]
        private RushAtkState _atkState = default;

        public new RusherEnemy Init()
        {
            base.Init();
            _idleState.Init(this);
            _atkState.Init(this);
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

            Fsm.ChangeState(EnemyStates.Attack, true);
        }
    }
}

