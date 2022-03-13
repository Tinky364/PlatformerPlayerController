using Godot;
using AI.States;

namespace AI.Enemies
{
    public class RusherEnemy : Enemy
    {
        [Export]
        private IdleState _idleState = default;
        [Export]
        private RushAtkState _atkState = default;

        public override void _Ready()
        {
            base._Ready();
            _idleState.Initialize(this, EnemyStates.Idle);
            _atkState.Initialize(this, EnemyStates.Attack);
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

            Fsm.SetCurrentState(EnemyStates.Attack, true);
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            _idleState.ExitTree();
            _atkState.ExitTree();
        }
    }
}

