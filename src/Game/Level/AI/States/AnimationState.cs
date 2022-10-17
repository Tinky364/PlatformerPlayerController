using CustomRegister;
using Godot;
using Game.Fsm;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.AI.States
{
    [Register]
    public class AnimationState : State<Enemy, Enemy.EnemyStates>
    {
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
        
        private string _curAnimationName;
        private float _count = 0;
        private bool _animationStarted = false;

        public override async void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            
            if (_animationSymmetrical) SetSymmetricalAnimation();
            
            Owner.Agent.Velocity.x = 0f;
            Owner.Agent.Direction.x = Owner.Agent.DirectionToTarget().x;
            
            // Waits before the animation.
            if (_waitBeforeAnimationDur != 0f) await TreeTimer.Singleton.Wait(_waitBeforeAnimationDur);

            if (_animationDuration == 0f) Owner.PlayAnimation(_curAnimationName);
            else Owner.PlayAnimation(_curAnimationName, _animationDuration);
            _animationStarted = true;
        }

        public override void PhysicsProcess(float delta)
        {
            if (_animationStarted && !Owner.AnimPlayer.CurrentAnimation.Equals(_curAnimationName))
            {
                if (_count < _waitAfterAnimationDur) _count += delta; // Waits after the animation.
                else Owner.Fsm.StopCurrentState(); // Ends the state.
            }
        }

        public override void Exit()
        {
            _curAnimationName = _animationName;
            _animationStarted = false;
            _count = 0f;
        }

        public override void Process(float delta) { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }

        private void SetSymmetricalAnimation()
        {
            if (Owner.Agent.Direction.x >= 0f) _curAnimationName = _animationName + "_r";
            else _curAnimationName = _animationName + "_l";
        }
    }
}