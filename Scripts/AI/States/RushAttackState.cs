using System.Threading;
using Godot;
using NavTool;

namespace AI.States
{
    public class RushAttackState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;
        
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitBeforeRushSec = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterRushSec = 1f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _rushSpeed = 100f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _waitAfterCollisionSec = 2f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _collisionBackWidth = 24f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _collisionBackSec = 1f;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRushing;
        
        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Attack);
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
            Events.Singleton.Connect("Damaged", this, nameof(OnTargetHit));
        }

        public override void Enter()
        {
            if (_enemy.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(RushAttackState)}");

            _enemy.Fsm.IsStateLocked = true;
            _enemy.AnimatedSprite.Play("idle");
            _enemy.Velocity.x = 0;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            Attack(_cancellationTokenSource.Token);
        }

        private async void Attack(CancellationToken cancellationToken)
        {
            // Waits before calculating the target position.
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitBeforeRushSec / 2f), "timeout");
            // Calculates the target position and sets its own direction.
            Vector2 targetPos = new Vector2(0, _enemy.NavPos.y);
            if (_enemy.DirectionToTarget().x >= 0)
            {
                _enemy.Direction = 1;
                targetPos.x = _enemy.NavArea.AreaRect.End.x - 16f;
            }
            else
            {
                _enemy.Direction = -1;
                targetPos.x = _enemy.NavArea.AreaRect.Position.x + 16f;
            }
            // Waits before rushing to the target position.
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitBeforeRushSec / 2f), "timeout");
            _enemy.AnimatedSprite.Play("run");
            // Starts rushing to the target position.
            _enemy.NavTween.MoveToward(NavTween.TweenMode.X, null, targetPos, _rushSpeed);
            _isRushing = true;
            // Waits until rushing ends.
            await ToSignal(_enemy.NavTween, "MoveCompleted");
            // When rushing ends before its duration
            if (cancellationToken.IsCancellationRequested) return;
            _isRushing = false;
            _enemy.AnimatedSprite.Play("idle");
            // Waits before changing state.
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitAfterRushSec), "timeout");
            _enemy.Fsm.IsStateLocked = false;
            _enemy.Fsm.SetCurrentState(Enemy.EnemyStates.Idle);
        }
        
        private async void Collision(Vector2 hitNormal)
        {
            _enemy.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                _enemy.GlobalPosition - hitNormal * _collisionBackWidth,
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
            if (!_isRushing) return;
            
            _isRushing = false;
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
