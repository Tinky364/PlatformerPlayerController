using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class AttackState : State<Enemy.EnemyStates>
    {
        private Enemy _enemy;
        private Tween _tween;

        [Export(PropertyHint.Range, "0,100,or_greater")]
        private float _backMoveDistance = 10f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _backMoveSecond = 0.75f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _jumpSecond = 0.5f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _landingMoveDistance = 3f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _landingMoveSecond = 0.2f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _afterAttackSecond = 1f;

        public void Initialize(Enemy enemy)
        {
            Initialize(Enemy.EnemyStates.Attack);
            _enemy = enemy;
            _enemy.Fsm.AddState(this);
            
            _tween = new Tween();
            _enemy.AddChild(_tween);
            _tween.Name = "AttackTween";
            _tween.PlaybackProcessMode = Tween.TweenProcessMode.Physics;
        }

        public override void Enter()
        {
            GD.Print($"{_enemy.Name}: AttackState");
            _enemy.Fsm.IsStateLocked = true;
            _enemy.AnimatedSprite.Play("idle");
            Attack();
        }
        
        private async void Attack()
        {
            float targetPosX = _enemy.NavArea.TargetNavBody.NavPosition.x;
            Vector2 dirToTarget = _enemy.NavArea.DirectionToTarget();
            _enemy.Velocity.x = 0;
            if (dirToTarget.x >= 0)
                _enemy.Direction = 1;
            else
                _enemy.Direction = -1;
            
            _tween.InterpolateProperty(
                _enemy.Body,
                "position:x",
                null,
                _enemy.NavBody.NavPosition.x + -dirToTarget.x * _backMoveDistance,
                _backMoveSecond,
                Tween.TransitionType.Quad
            );
            _tween.Start();
            
            await ToSignal(_tween, "tween_completed");
            
            _enemy.AnimatedSprite.Play("run");
            _enemy.Velocity.y = -_enemy.Gravity * _jumpSecond / 2f;
            _tween.InterpolateProperty(
                _enemy.Body,
                "position:x",
                null,
                targetPosX,
                _jumpSecond
            );
            _tween.Start();
            
            await ToSignal(_tween, "tween_completed");
            
            _tween.InterpolateProperty(
                _enemy.Body,
                "position:x",
                null,
                targetPosX + dirToTarget.x * _landingMoveDistance,
                _landingMoveSecond,
                Tween.TransitionType.Quad,
                Tween.EaseType.Out
            );
            _tween.Start();
            
            await ToSignal(_tween, "tween_completed");
            _enemy.AnimatedSprite.Play("idle");
            await ToSignal(GameManager.Singleton.Tree.CreateTimer(_afterAttackSecond), "timeout");
            
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