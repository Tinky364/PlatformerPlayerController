using Godot;
using System;

namespace StateMachine
{
    public class JumperEnemy : Enemy
    {
        [Export]
        private IdleState _idleState;
        [Export]
        private ChaseState _chaseState;
        [Export]
        private AttackState _attackState;

        public override void _Ready()
        {
            base._Ready();
            _idleState.Initialize(this);
            _attackState.Initialize(this);
            _chaseState.Initialize(this);
            Fsm.SetCurrentState(EnemyStates.Idle);
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            StateController();
            GD.Print(Fsm.CurrentState.Id);
        }

        private void StateController()
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
