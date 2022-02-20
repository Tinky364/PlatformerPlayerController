using Godot;
using Manager;
using NavTool;

namespace AI
{
    public abstract class Enemy : Node2D
    {
        public StateMachine<EnemyStates> Fsm { get; private set; }
        public NavAgent2D Agent { get; private set; }
        public AnimatedSprite AnimatedSprite { get; private set; }

        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity { get; private set; } = 600f;
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
            Agent.Connect("ScreenEntered", this, nameof(OnScreenEnter));
            Agent.Connect("ScreenExited", this, nameof(OnScreenExit));
            Fsm = new StateMachine<EnemyStates>();
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
            
            Agent.Velocity = Agent.MoveInArea(Agent.Velocity, delta, Vector2.Up);
            if (!Agent.IsOnFloor()) Agent.Velocity.y += Gravity * delta;
            else if (!Agent.SnapDisabled) Agent.Velocity.y = Gravity * delta;
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

        private void SetTarget(NavBody2D target)
        {
            if (Agent.TargetNavBody != null) return;
            Agent.TargetNavBody = target;
        }
        
        protected void OnScreenEnter() => GM.S.SetNodeActive(this, true);
        
        protected void OnScreenExit() => GM.S.SetNodeActive(this, false);
    }
}
