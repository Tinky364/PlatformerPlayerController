using System.Threading;
using CustomRegister;
using Game.Fsm;
using Godot;
using Game.Service;
using Game.Service.Debug;
using NavTool;

namespace Game.Level.AI.States
{
    [Register]
    public class JumpAtkState : State<Enemy, Enemy.EnemyStates>
    {
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

        public override void Init(Enemy owner)
        {
            base.Init(owner);
            Events.Singleton.Connect(nameof(Events.Damaged), this, nameof(OnTargetHit));
        }

        public override void ExitTree()
        {
            Events.Singleton.Disconnect(nameof(Events.Damaged), this, nameof(OnTargetHit));
        }

        public override void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            
            Owner.PlayAnimation("idle");
            Owner.Agent.Velocity.x = 0;
            
            Vector2 dirToTarget = Owner.Agent.DirectionToTarget();
            Owner.Agent.Direction.x = dirToTarget.x;
            
            _cancellationTokenSource = new CancellationTokenSource();
            Attack(dirToTarget, Owner.Agent.TargetNavBody.NavPos, _cancellationTokenSource.Token);
        }
        
        public override void Process(float delta) { }
        public override void PhysicsProcess(float delta) { }
        public override void Exit() { }
        public override bool CanChange() { return false; }

        private async void Attack(Vector2 dirToTarget, Vector2 targetPos, CancellationToken token)
        {
            float backMoveDist = Mathf.Clamp(
                _backMoveDistMax - Owner.Agent.DistanceTo(targetPos), _backMoveDistMin, _backMoveDistMax
            );
            await TreeTimer.Singleton.Wait(_waitBeforeAttackDur);
            Owner.Agent.NavTween.MoveToward(
                NavTween.TweenMode.X, null, Owner.Agent.NavPos - dirToTarget * backMoveDist,
                Owner.MoveSpeed, Tween.TransitionType.Quad
            );
            await ToSignal(Owner.Agent.NavTween, "MoveCompleted");
            Owner.PlayAnimation("run");
            Owner.Agent.SnapDisabled = true;
            _isJumping = true;
            Owner.Agent.Velocity.y = -Owner.Gravity * _jumpDur / 2f;
            Owner.Agent.NavTween.MoveLerp(NavTween.TweenMode.X, null, targetPos, _jumpDur);
            await ToSignal(Owner.Agent.NavTween, "MoveCompleted");
            if (token.IsCancellationRequested) return;
            Owner.Agent.SnapDisabled = false;
            _isJumping = false;
            Owner.Agent.NavTween.MoveLerp(
                NavTween.TweenMode.X, null, targetPos + dirToTarget * _landingMoveDist,
                _landingMoveDur, Tween.TransitionType.Quad, Tween.EaseType.Out
            );
            await ToSignal(Owner.Agent.NavTween, "MoveCompleted");
            Owner.PlayAnimation("idle");
            await TreeTimer.Singleton.Wait(_waitAfterAttackDur);
            Owner.Fsm.StopCurrentState();
        }
        
        private async void Collision(Vector2 hitNormal)
        {
            Owner.Agent.NavTween.MoveLerp(
                NavTween.TweenMode.X, null, Owner.Agent.NavPos - hitNormal * _collisionBackWidth,
                _collisionBackDur, Tween.TransitionType.Cubic, Tween.EaseType.Out
            );
            await ToSignal(Owner.Agent.NavTween, "MoveCompleted");
            await TreeTimer.Singleton.Wait(_waitAfterCollisionDur);
            Owner.Fsm.StopCurrentState();
        }

        private void OnTargetHit(
            NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (attacker != Owner.Agent || target != Owner.Agent.TargetNavBody) return;
            if (Owner.Fsm.CurrentState != this || !_isJumping) return;
            _isJumping = false;
            Owner.Agent.SnapDisabled = false;
            _cancellationTokenSource?.Cancel();
            Owner.Agent.NavTween.StopMove();
            Collision(hitNormal);
        }
    }
}