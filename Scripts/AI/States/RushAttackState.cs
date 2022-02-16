using System.Threading;
using Godot;
using Manager;
using NavTool;

namespace AI.States
{
    public class RushAttackState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;
        
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
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
            Events.Singleton.Connect("Damaged", this, nameof(OnTargetHit));
        }

        public override void Enter()
        {
            if (_enemy.Body.DebugEnabled) GD.Print($"{_enemy.Name}: {nameof(RushAttackState)}");

            _enemy.AnimatedSprite.Play("idle");
            _enemy.Body.Velocity.x = 0;

            _attackCancel = new CancellationTokenSource();
            Attack(_attackCancel.Token);
        }

        private async void Attack(CancellationToken cancellationToken)
        {
            // Waits before calculating the target position.
            await ToSignal(_enemy.GetTree().CreateTimer(_waitBeforeRushDur / 2f), "timeout");
            // Calculates the target position and sets its own direction.
            Vector2 targetPos = new Vector2(0, _enemy.Body.NavPos.y);
            if (_enemy.Body.DirectionToTarget().x >= 0)
            {
                _enemy.Body.Direction = 1;
                targetPos.x = _enemy.Body.NavArea.AreaRect.End.x - 12f;
            }
            else
            {
                _enemy.Body.Direction = -1;
                targetPos.x = _enemy.Body.NavArea.AreaRect.Position.x + 12f;
            }
            // Waits before rushing to the target position.
            await ToSignal(_enemy.GetTree().CreateTimer(_waitBeforeRushDur / 2f), "timeout");
            _enemy.AnimatedSprite.Play("run");
            // Starts rushing to the target position.
            _enemy.Body.NavTween.MoveToward(
                NavTween.TweenMode.X, null, targetPos, _rushSpeed, Tween.TransitionType.Cubic
            );
            _isRushing = true;
            // Waits until rushing ends.
            await ToSignal(_enemy.Body.NavTween, "MoveCompleted");
            // When rushing ends before its duration
            if (cancellationToken.IsCancellationRequested) return;
            _isRushing = false;
            _enemy.AnimatedSprite.Play("idle");
            // Waits before changing state.
            await ToSignal(_enemy.GetTree().CreateTimer(_waitAfterRushDur), "timeout");
            _enemy.Fsm.StopCurrentState();
        }
        
        private async void Collision(Vector2 hitNormal)
        {
            _enemy.Body.NavTween.MoveLerp(
                NavTween.TweenMode.X,
                null,
                _enemy.GlobalPosition - hitNormal * _collisionBackWidth,
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
            if (_enemy.Fsm.CurrentState != this || !_isRushing) return;
            
            _isRushing = false;
            _attackCancel?.Cancel();
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
