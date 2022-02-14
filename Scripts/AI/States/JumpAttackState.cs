using System.Threading;
using Godot;
using NavTool;

namespace AI.States
{
    public class JumpAttackState : State<Enemy.EnemyStates>
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
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterCollisionSec = 2f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _collisionBackWidth = 24f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _collisionBackSec = 1f;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isJumping;

        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Attack);
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
            Events.Singleton.Connect("Damaged", this, nameof(OnTargetHit));
        }

        public override void Enter()
        {
            if (_enemy.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(JumpAttackState)}");
            
            _enemy.Fsm.IsStateLocked = true;
            _enemy.AnimatedSprite.Play("idle");
            _enemy.Velocity.x = 0;
            
            Vector2 dirToTarget = _enemy.DirectionToTarget();
            _enemy.Direction = dirToTarget.x >= 0 ? 1 : -1;
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            Attack(dirToTarget, _enemy.TargetNavBody.NavPos, _cancellationTokenSource.Token);
        }
        
        private async void Attack(Vector2 dirToTarget, Vector2 targetPos, CancellationToken token)
        {
            float backMoveDist = Mathf.Clamp(
                _backMoveDistMax - _enemy.DistanceTo(targetPos), _backMoveDistMin, _backMoveDistMax
            );
            float backMoveSec = backMoveDist * _backMoveSec / _backMoveDistMin;
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitBeforeAttackSec), "timeout");
            _enemy.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                _enemy.NavPos - dirToTarget * backMoveDist,
                backMoveSec,
                Tween.TransitionType.Quad
            );
            await ToSignal(_enemy.NavTween, "MoveCompleted");
            _enemy.AnimatedSprite.Play("run");
            _isJumping = true;
            _enemy.Velocity.y = -_enemy.Gravity * _jumpSec / 2f;
            _enemy.NavTween.MoveLerp(NavTween.TweenMode.X, null, targetPos, _jumpSec);
            await ToSignal(_enemy.NavTween, "MoveCompleted");
            _isJumping = false;
            if (token.IsCancellationRequested) return;
            _enemy.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                targetPos + dirToTarget * _landingMoveDist,
                _landingMoveSec,
                Tween.TransitionType.Quad,
                Tween.EaseType.Out
            );
            await ToSignal(_enemy.NavTween, "MoveCompleted");
            _enemy.AnimatedSprite.Play("idle");
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitAfterAttackSec), "timeout");
            _enemy.Fsm.IsStateLocked = false;
            _enemy.Fsm.SetCurrentState(Enemy.EnemyStates.Idle);
        }
        
        private async void Collision(Vector2 hitNormal)
        {
            _enemy.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                _enemy.NavPos - hitNormal * _collisionBackWidth,
                _collisionBackSec,
                Tween.TransitionType.Cubic,
                Tween.EaseType.Out
            );
            await ToSignal(_enemy.NavTween, "MoveCompleted");
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitAfterCollisionSec), "timeout");
            _enemy.Fsm.IsStateLocked = false;
            _enemy.Fsm.SetCurrentState(Enemy.EnemyStates.Idle);
        }
        
        private void OnTargetHit(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (attacker != _enemy || target != _enemy.TargetNavBody) return;
            if (_enemy.Fsm.CurrentState != this) return;
            if (!_isJumping) return;
            
            _isJumping = false;
            _cancellationTokenSource?.Cancel();
            _enemy.NavTween.StopMove();
            _cancellationTokenSource = null;
            Collision(hitNormal);
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