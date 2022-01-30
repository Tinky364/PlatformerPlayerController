using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class AttackState : State<Enemy.EnemyStates>
    {
        private readonly Enemy _enemy;
        private Tween _tween;

        protected AttackState() {}

        public AttackState(Enemy enemy) : base(Enemy.EnemyStates.Attack)
        {
            _enemy = enemy;
            _tween = new Tween();
            _enemy.AddChild(_tween);
            _tween.Name = "AttackTween";
        }

        public override void Enter()
        {
            GD.Print("AttackState");
            _enemy.Machine.IsStateLocked = true;

           
            Attack();
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

        private async void Attack()
        {
            if (_enemy.IsOnGround && _enemy.NavArea.IsTargetReachable)
            {
                _tween.PlaybackProcessMode = Tween.TweenProcessMode.Physics;

                _tween.InterpolateProperty(
                    _enemy.Body,
                    "position:x",
                    _enemy.NavBody.NavPosition.x,
                    _enemy.NavBody.NavPosition.x + -_enemy.NavArea.DirectionToTarget().x * 10f,
                    1f,
                    Tween.TransitionType.Quad
                );
                
                _tween.Start();

                await ToSignal(_tween, "tween_completed");

                float sec = 0.5f;
                _enemy.Velocity.y = -_enemy.Gravity * sec / 2f;

                _tween.InterpolateProperty(
                    _enemy.Body,
                    "position:x",
                    _enemy.NavBody.NavPosition.x,
                    _enemy.NavArea.TargetNavBody.NavPosition.x,
                    sec
                );
                _tween.Start();
            }
        }

        private void OnTweenEnded(Object obj, NodePath key)
        {
            if (obj == _enemy.Body && key == ":position:y")
            {
                GD.Print("b");
            }
        }
    }
}