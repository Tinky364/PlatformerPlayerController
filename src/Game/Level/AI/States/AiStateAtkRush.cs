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
    public class AiStateAtkRush : State<Enemy, Enemy.EnemyStates>
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitBeforeRushDur = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterRushDur = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _rushSpeed = 50f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _collisionBackWidth = 24f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _collisionBackDur = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterCollisionDur = 2f;
        
        private CancellationTokenSource _attackCancel;
        private bool _isRushing;

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

            _attackCancel = new CancellationTokenSource();
            Attack(_attackCancel.Token);
        }
        
        public override void Process(float delta) { }
        public override void PhysicsProcess(float delta) { }
        public override void Exit() { }
        public override bool CanChange() { return false; }

        private async void Attack(CancellationToken cancellationToken)
        {
            // Waits before calculating the target position.
            await TreeTimer.Singleton.Wait(_waitBeforeRushDur / 2f);
            // Calculates the target position and sets its own direction.
            Vector2 targetPos = new Vector2(0, Owner.Agent.NavPos.y);
            if (Owner.Agent.DirectionToTarget().x >= 0)
            {
                Owner.Agent.Direction.x = 1;
                targetPos.x = Owner.Agent.NavArea.AreaRect.End.x - 12f;
            }
            else
            {
                Owner.Agent.Direction.x = -1;
                targetPos.x = Owner.Agent.NavArea.AreaRect.Position.x + 12f;
            }
            // Waits before rushing to the target position.
            await TreeTimer.Singleton.Wait(_waitBeforeRushDur / 2f);
            Owner.PlayAnimation("run");
            // Starts rushing to the target position.
            Owner.Agent.NavTween.MoveToward(
                NavTween.TweenMode.X, null, targetPos, _rushSpeed, Tween.TransitionType.Cubic
            );
            _isRushing = true;
            // Waits until rushing ends.
            await ToSignal(Owner.Agent.NavTween, "MoveCompleted");
            // When rushing ends before its duration
            if (cancellationToken.IsCancellationRequested) return;
            _isRushing = false;
            Owner.PlayAnimation("idle");
            // Waits before changing state.
            await TreeTimer.Singleton.Wait(_waitAfterRushDur);
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

        private void OnTargetHit(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (attacker != Owner.Agent || target != Owner.Agent.TargetNavBody) return;
            if (Owner.Fsm.CurrentState != this || !_isRushing) return;
            _isRushing = false;
            _attackCancel?.Cancel();
            Owner.Agent.NavTween.StopMove();
            Collision(hitNormal);
        }
    }
}
