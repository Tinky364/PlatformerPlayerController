using System.Threading;
using Godot;
using Manager;
using NavTool;

namespace AI.States
{
    public class RushAttackState : State<Enemy.EnemyStates>
    {
        private Enemy E { get; set; }
        
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

            _attackCancel = new CancellationTokenSource();
            Attack(_attackCancel.Token);
        }

        private async void Attack(CancellationToken cancellationToken)
        {
            // Waits before calculating the target position.
            await TreeTimer.S.Wait(_waitBeforeRushDur / 2f);
            // Calculates the target position and sets its own direction.
            Vector2 targetPos = new Vector2(0, E.Agent.NavPos.y);
            if (E.Agent.DirectionToTarget().x >= 0)
            {
                E.Agent.Direction.x = 1;
                targetPos.x = E.Agent.NavArea.AreaRect.End.x - 12f;
            }
            else
            {
                E.Agent.Direction.x = -1;
                targetPos.x = E.Agent.NavArea.AreaRect.Position.x + 12f;
            }
            // Waits before rushing to the target position.
            await TreeTimer.S.Wait(_waitBeforeRushDur / 2f);
            E.AnimatedSprite.Play("run");
            // Starts rushing to the target position.
            E.Agent.NavTween.MoveToward(
                NavTween.TweenMode.X, null, targetPos, _rushSpeed, Tween.TransitionType.Cubic
            );
            _isRushing = true;
            // Waits until rushing ends.
            await ToSignal(E.Agent.NavTween, "MoveCompleted");
            // When rushing ends before its duration
            if (cancellationToken.IsCancellationRequested) return;
            _isRushing = false;
            E.AnimatedSprite.Play("idle");
            // Waits before changing state.
            await TreeTimer.S.Wait(_waitAfterRushDur);
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
            if (E.Fsm.CurrentState != this || !_isRushing) return;
            _isRushing = false;
            _attackCancel?.Cancel();
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
