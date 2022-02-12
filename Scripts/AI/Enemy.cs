using Godot;
using NavTool;

namespace AI
{
    public abstract class Enemy : NavBody2D
    {
        public StateMachine<EnemyStates> Fsm { get; } = new StateMachine<EnemyStates>();
        public AnimatedSprite AnimatedSprite;

        [Export(PropertyHint.Range, "0,200,or_greater")]
        public float MoveSpeed = 15f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _damageValue = 1;
        
        public enum EnemyStates { Idle, Chase, Attack }

        public override void _Ready()
        {
            base._Ready();
            AnimatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            StateController();
            Fsm._Process(delta);
            AnimationController();
        }
        
        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            CheckGround();
            Fsm._PhysicsProcess(delta);
            if (!IsOnGround) Velocity.y += Gravity * delta; // Adds gravity force increasingly.
            if (IsLerping) Velocity.x = CalculateLerpMotion(delta); 
            Velocity = MoveAndSlideInArea(Velocity, delta, Vector2.Up);
        }

        protected abstract void StateController();
        
        private void CheckGround() => IsOnGround = GroundRay.Count > 0;

        private void AnimationController()
        {
            switch (Direction)
            {
                case 1:
                    AnimatedSprite.FlipH = false;
                    break;
                case -1:
                    AnimatedSprite.FlipH = true;
                    break;
            }
        }
    }
}
