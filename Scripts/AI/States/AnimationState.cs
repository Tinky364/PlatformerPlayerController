using Godot;
using Manager;

namespace AI.States
{
    public class AnimationState : State<Enemy.EnemyStates>
    {
        [Export]
        private Enemy.EnemyStates State { get; set; }
        [Export]
        private string _animationName = "FILL IT!!!";
        [Export]
        private bool _animationSymmetrical = false;
        [Export(PropertyHint.Range, "0,20,0.1,or_greater")]
        private float _waitBeforeAnimationDur = 0.2f;
        [Export(PropertyHint.Range, "0,20,0.1,or_greater")]
        private float _waitAfterAnimationDur = 1f;
        [Export(PropertyHint.Range, "0,20,0.05,or_greater")]
        private float _animationDuration = 0f;
        
        private Enemy E { get; set; }
        private string _curAnimationName;
        private float _count = 0;
        private bool _animationStarted = false;

        public void Initialize(Enemy enemy)
        {
            Initialize(State);
            E = enemy;
            E.Fsm.AddState(this);
        }

        public override async void Enter()
        {
            GM.Print(E.Agent.DebugEnabled, $"{E.Name}: {Key}");
            
            if (_animationSymmetrical) SetSymmetricalAnimation();
            
            E.Agent.Velocity.x = 0f;
            E.Agent.Direction.x = E.Agent.DirectionToTarget().x;
            
            // Waits before the animation.
            if (_waitBeforeAnimationDur != 0f) await TreeTimer.S.Wait(_waitBeforeAnimationDur);

            if (_animationDuration == 0f) E.PlayAnimation(_curAnimationName);
            else E.PlayAnimation(_curAnimationName, _animationDuration);
            _animationStarted = true;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            if (_animationStarted && !E.AnimPlayer.CurrentAnimation.Equals(_curAnimationName))
            {
                if (_count < _waitAfterAnimationDur) _count += delta; // Waits after the animation.
                else E.Fsm.StopCurrentState(); // Ends the state.
            }
        }

        public override void Exit()
        {
            _curAnimationName = _animationName;
            _animationStarted = false;
            _count = 0f;
        }

        private void SetSymmetricalAnimation()
        {
            if (E.Agent.Direction.x >= 0f) _curAnimationName = _animationName + "_r";
            else _curAnimationName = _animationName + "_l";
        }
    }
}