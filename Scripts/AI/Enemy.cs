using Godot;
using Manager;
using NavTool;

namespace AI
{
    public abstract class Enemy : Node2D
    {
        public StateMachine<EnemyStates> Fsm { get; } = new StateMachine<EnemyStates>();
        public NavAgent2D Agent { get; private set; }
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

        public override void _EnterTree()
        {
            base._EnterTree();
            AddToGroup("Enemy");
        }

        public override void _Ready()
        {
            base._Ready();
            Agent = GetNode<NavAgent2D>("NavAgent2D");
            AnimatedSprite = GetNode<AnimatedSprite>("NavAgent2D/AnimatedSprite");
            Agent.Connect("BodyColliding", this, nameof(OnBodyColliding));
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            StateController();
            Fsm._Process(delta);
            DirectionControl();
        }
        
        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            Fsm._PhysicsProcess(delta);
            if (!Agent.IsOnFloor()) Agent.Velocity.y += Gravity * delta; // Adds gravity force increasingly.
            else if (!Agent.SnapDisabled) Agent.Velocity.y = Gravity * delta;
            Agent.Velocity = Agent.MoveInArea(Agent.Velocity, delta, Vector2.Up);
        }

        private void OnBodyColliding(Node body)
        {
            if (!(body is NavBody2D targetNavBody)) return;
            if (targetNavBody.IsUnhurtable) return;
            Events.S.EmitSignal(
                "Damaged",
                targetNavBody,
                _damageValue,
                Agent,
                Agent.GlobalPosition.DirectionTo(targetNavBody.GlobalPosition)
            );
        }

        protected abstract void StateController();
        
        private void DirectionControl()
        {
            switch (Agent.Direction.x)
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
