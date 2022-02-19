using System.Threading;
using Godot;
using Manager;
using NavTool;

namespace AI.States
{
    public class JumpAttackState : State<Enemy.EnemyStates>
    {
        private Enemy E { get; set; }

        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitBeforeAttackDur = 1f;
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
            E = enemy;
            E.Fsm.AddState(this);
            Events.S.Connect("Damaged", this, nameof(OnTargetHit));
        }

        public override void Enter()
        {
            if (E.Agent.DebugEnabled) GD.Print($"{E.Name}: {Key}");
            
            E.AnimatedSprite.Play("idle");
            E.Agent.Velocity.x = 0;
            
            Vector2 dirToTarget = E.Agent.DirectionToTarget();
            E.Agent.Direction.x = dirToTarget.x;
            
            _cancellationTokenSource = new CancellationTokenSource();
            Attack(dirToTarget, E.Agent.TargetNavBody.NavPos, _cancellationTokenSource.Token);
        }
        
        private async void Attack(Vector2 dirToTarget, Vector2 targetPos, CancellationToken token)
        {
            float backMoveDist = Mathf.Clamp(
                _backMoveDistMax - E.Agent.DistanceTo(targetPos), _backMoveDistMin, _backMoveDistMax
            );
            await TreeTimer.S.Wait(_waitBeforeAttackDur);
            E.Agent.NavTween.MoveToward(
                NavTween.TweenMode.X,
                null,
                E.Agent.NavPos - dirToTarget * backMoveDist,
                E.MoveSpeed,
                Tween.TransitionType.Quad
            );
            await ToSignal(E.Agent.NavTween, "MoveCompleted");
            E.AnimatedSprite.Play("run");
            E.Agent.SnapDisabled = true;
            _isJumping = true;
            E.Agent.Velocity.y = -E.Gravity * _jumpDur / 2f;
            E.Agent.NavTween.MoveLerp(NavTween.TweenMode.X, null, targetPos, _jumpDur);
            await ToSignal(E.Agent.NavTween, "MoveCompleted");
            if (token.IsCancellationRequested) return;
            E.Agent.SnapDisabled = false;
            _isJumping = false;
            E.Agent.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                targetPos + dirToTarget * _landingMoveDist,
                _landingMoveDur,
                Tween.TransitionType.Quad,
                Tween.EaseType.Out
            );
            await ToSignal(E.Agent.NavTween, "MoveCompleted");
            E.AnimatedSprite.Play("idle");
            await TreeTimer.S.Wait(_waitAfterAttackDur);
            E.Fsm.StopCurrentState();
        }
        
        private async void Collision(Vector2 hitNormal)
        {
            E.Agent.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                E.Agent.NavPos - hitNormal * _collisionBackWidth,
                _collisionBackDur,
                Tween.TransitionType.Cubic,
                Tween.EaseType.Out
            );
            await ToSignal(E.Agent.NavTween, "MoveCompleted");
            await TreeTimer.S.Wait(_waitAfterCollisionDur);
            E.Fsm.StopCurrentState();
        }
        
        private void OnTargetHit(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (attacker != E.Agent || target != E.Agent.TargetNavBody) return;
            if (E.Fsm.CurrentState != this || !_isJumping) return;
            _isJumping = false;
            E.Agent.SnapDisabled = false;
            _cancellationTokenSource?.Cancel();
            E.Agent.NavTween.StopMove();
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