using System.Threading;
using Godot;

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
            Events.Singleton.Connect("PlayerHealthChanged", this, nameof(OnPlayerHit));
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
            Vector2 dirToTarget = _enemy.NavArea.DirectionToTarget();
            Vector2 targetPos;
            if (dirToTarget.x >= 0)
            {
                _enemy.Direction = 1;
                targetPos = _enemy.NavArea.ReachableAreaRect.End;
            }
            else
            {
                _enemy.Direction = -1;
                targetPos = _enemy.NavArea.ReachableAreaRect.Position;
            }
            // Waits before rushing to the target position.
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitBeforeRushSec / 2f), "timeout");
            _enemy.AnimatedSprite.Play("run");
            // Starts rushing to the target position.
            _enemy.NavChar.LerpWithSpeed(
                targetPos.x,
                _rushSpeed,
                Tween.TransitionType.Quad
            );
            _isRushing = true;
            // Waits until rushing ends.
            await ToSignal(_enemy.NavChar.Tween, "tween_completed");
            // When rushing ends before its duration
            if (cancellationToken.IsCancellationRequested) return;
            _isRushing = false;
            _enemy.AnimatedSprite.Play("idle");
            // Waits before changing state.
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitAfterRushSec), "timeout");
            _enemy.Fsm.IsStateLocked = false;
            _enemy.Fsm.SetCurrentState(Enemy.EnemyStates.Idle);
        }
        
        private async void Collision()
        {
            _enemy.NavChar.LerpWithDuration(
                _enemy.NavChar.GlobalPosition.x - _enemy.Direction * _collisionBackWidth,
                _collisionBackSec,
                Tween.TransitionType.Cubic,
                Tween.EaseType.Out
            );
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_waitAfterCollisionSec), "timeout");
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

        private void OnPlayerHit(int newHealth, int maxHealth, Enemy attacker)
        {
            if (attacker != _enemy) return;
            if (_enemy.Fsm.CurrentState != this) return;
            if (!_isRushing) return;
            
            _isRushing = false;
            _cancellationTokenSource?.Cancel();
            _enemy.NavChar.StopLerp();
            _cancellationTokenSource = null;
            Collision();
        }
    }
}
