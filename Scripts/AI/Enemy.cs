using Godot;
using Manager;
using NavTool;

namespace AI
{
    public abstract class Enemy : NavBody2D
    {
        public StateMachine<EnemyStates> Fsm { get; } = new StateMachine<EnemyStates>();
        public AnimatedSprite AnimatedSprite;

        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity = 600f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        public float MoveSpeed { get; private set; } = 15f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        public float MoveAcceleration { get; private set; } = 100f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _damageValue = 1;
        
        public enum EnemyStates { Idle, Chase, Attack }

        public override void _Ready()
        {
            base._Ready();
            AnimatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
            Connect(nameof(BodyColliding), this, nameof(OnBodyColliding));
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
            Velocity = MoveAndSlideInArea(Velocity, delta, Vector2.Up);
        }

        private void OnBodyColliding(Node body)
        {
            if (!(body is NavBody2D targetNavBody)) return;
            if (targetNavBody.IsUnhurtable) return;
            Events.Singleton.EmitSignal(
                "Damaged",
                targetNavBody,
                _damageValue,
                this,
                GlobalPosition.DirectionTo(targetNavBody.GlobalPosition)
            );
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
