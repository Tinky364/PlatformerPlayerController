using Godot;

namespace StateMachine
{
    public class AttackState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;

        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitBeforeAttackSec = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _backMoveSec = 0.4f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _jumpSec = 0.6f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _landingMoveSec = 0.2f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterAttackSec = 1f;
        [Export(PropertyHint.Range, "0,100,or_greater")]
        private float _backMoveDistMin = 5f;
        [Export(PropertyHint.Range, "0,100,or_greater")]
        private float _backMoveDistMax = 30f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _landingMoveDist = 3f;

        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Attack);
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
        }

        public override void Enter()
        {
            if (_enemy.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(AttackState)}");
            
            _enemy.Fsm.IsStateLocked = true;
            _enemy.AnimatedSprite.Play("idle");
            _enemy.Velocity.x = 0;
            
            Vector2 dirToTarget = _enemy.NavArea.DirectionToTarget();
            _enemy.Direction = dirToTarget.x >= 0 ? 1 : -1;
            
            Attack(dirToTarget, _enemy.NavArea.TargetNavChar.NavPosition);
        }
        
        private async void Attack(Vector2 dirToTarget, Vector2 targetPos)
        {
            float backMoveDist = Mathf.Clamp(
                _backMoveDistMax - _enemy.NavChar.DistanceTo(targetPos),
                _backMoveDistMin,
                _backMoveDistMax
            );
            float backMoveSec = backMoveDist * _backMoveSec / _backMoveDistMin;
            
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitBeforeAttackSec), "timeout");
            
            _enemy.NavChar.InterpolateMove(
                _enemy.NavChar.NavPosition.x + -dirToTarget.x * backMoveDist,
                backMoveSec,
                Tween.TransitionType.Quad
            );
            
            await ToSignal(_enemy.NavChar.Tween, "tween_completed");

            _enemy.AnimatedSprite.Play("run");
            _enemy.CanAttack = true;
            _enemy.Velocity.y = -_enemy.Gravity * _jumpSec / 2f;
            _enemy.NavChar.InterpolateMove(
                targetPos.x,
                _jumpSec
            );
            
            await ToSignal(_enemy.NavChar.Tween, "tween_completed");
            
            _enemy.CanAttack = false;
            _enemy.NavChar.InterpolateMove(
                targetPos.x + dirToTarget.x * _landingMoveDist,
                _landingMoveSec,
                Tween.TransitionType.Quad,
                Tween.EaseType.Out
            );
            
            await ToSignal(_enemy.NavChar.Tween, "tween_completed");
            
            _enemy.AnimatedSprite.Play("idle");
            
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitAfterAttackSec), "timeout");
            
            _enemy.Fsm.IsStateLocked = false;
            _enemy.Fsm.SetCurrentState(Enemy.EnemyStates.Idle);
        }
        
        public override void Exit()
        {
        }

        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
        }
    }
}