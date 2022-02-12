using Godot;
using AI.States;

namespace AI.Enemies
{
    public class RusherEnemy : Enemy
    {
        [Export]
        private IdleState _idleState;
        [Export]
        private RushAttackState _attackState;

        public override void _Ready()
        {
            base._Ready();
            _idleState.Initialize(this);
            _attackState.Initialize(this);
            Fsm.SetCurrentState(EnemyStates.Idle);
        }

        protected override void StateController()
        {
            if (Fsm.IsStateLocked) return;

            if (TargetNavBody.IsInactive || !NavArea.IsTargetReachable)
            {
                Fsm.SetCurrentState(EnemyStates.Idle);
                return;
            }

            Fsm.SetCurrentState(EnemyStates.Attack);
        }
    }
}

