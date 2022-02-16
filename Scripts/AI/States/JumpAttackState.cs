using System.Threading;
using Godot;
using Manager;
using NavTool;

namespace AI.States
{
    public class JumpAttackState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;

        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitBeforeAttackDur = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _backMoveDur = 0.4f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _jumpDur = 0.6f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _landingMoveDur = 0.2f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterAttackDur = 1f;
        [Export(PropertyHint.Range, "0,100,or_greater")]
        private float _backMoveDistMin = 15f;
        [Export(PropertyHint.Range, "0,100,or_greater")]
        private float _backMoveDistMax = 30f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _landingMoveDist = 3f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterCollisionDur = 2f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _collisionBackWidth = 24f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _collisionBackDur = 1f;

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
            if (_enemy.Body.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(JumpAttackState)}");
            
            _enemy.AnimatedSprite.Play("idle");
            _enemy.Body.Velocity.x = 0;
            
            Vector2 dirToTarget = _enemy.Body.DirectionToTarget();
            _enemy.Body.Direction = dirToTarget.x >= 0 ? 1 : -1;
            
            _cancellationTokenSource = new CancellationTokenSource();
            Attack(dirToTarget, _enemy.Body.TargetNavBody.NavPos, _cancellationTokenSource.Token);
        }
        
        private async void Attack(Vector2 dirToTarget, Vector2 targetPos, CancellationToken token)
        {
            float backMoveDist = Mathf.Clamp(
                _backMoveDistMax - _enemy.Body.DistanceTo(targetPos), _backMoveDistMin, _backMoveDistMax
            );
            await ToSignal(_enemy.GetTree().CreateTimer(_waitBeforeAttackDur), "timeout");
            _enemy.Body.NavTween.MoveToward(
                NavTween.TweenMode.X,
                null,
                _enemy.Body.NavPos - dirToTarget * backMoveDist,
                _enemy.MoveSpeed,
                Tween.TransitionType.Quad
            );
            await ToSignal(_enemy.Body.NavTween, "MoveCompleted");
            _enemy.AnimatedSprite.Play("run");
            _isJumping = true;
            _enemy.Body.Velocity.y = -_enemy.Gravity * _jumpDur / 2f;
            _enemy.Body.NavTween.MoveLerp(NavTween.TweenMode.X, null, targetPos, _jumpDur);
            await ToSignal(_enemy.Body.NavTween, "MoveCompleted");
            if (token.IsCancellationRequested) return;
            _isJumping = false;
            _enemy.Body.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                targetPos + dirToTarget * _landingMoveDist,
                _landingMoveDur,
                Tween.TransitionType.Quad,
                Tween.EaseType.Out
            );
            await ToSignal(_enemy.Body.NavTween, "MoveCompleted");
            _enemy.AnimatedSprite.Play("idle");
            await ToSignal(_enemy.GetTree().CreateTimer(_waitAfterAttackDur), "timeout");
            _enemy.Fsm.StopCurrentState();
        }
        
        private async void Collision(Vector2 hitNormal)
        {
            _enemy.Body.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                _enemy.Body.NavPos - hitNormal * _collisionBackWidth,
                _collisionBackDur,
                Tween.TransitionType.Cubic,
                Tween.EaseType.Out
            );
            await ToSignal(_enemy.Body.NavTween, "MoveCompleted");
            await ToSignal(_enemy.GetTree().CreateTimer(_waitAfterCollisionDur), "timeout");
            _enemy.Fsm.StopCurrentState();
        }
        
        private void OnTargetHit(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (attacker != _enemy.Body || target != _enemy.Body.TargetNavBody) return;
            if (_enemy.Fsm.CurrentState != this) return;
            if (!_isJumping) return;
            
            _isJumping = false;
            _cancellationTokenSource?.Cancel();
            _enemy.Body.NavTween.StopMove();
            Collision(hitNormal);
        }
        
        public override void Process(float delta)
        {
        }

        public override void PhysicsProcess(float delta)
        {
        }
        
        public override void Exit()
        {
        }
    }
}