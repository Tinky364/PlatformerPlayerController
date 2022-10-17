using Godot;
using Game.Fsm;
using Game.Service;
using NavTool;

namespace Game.Level.AI
{
    public abstract class Enemy : Node2D
    {
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity { get; private set; } = 600f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        public float MoveSpeed { get; private set; } = 15f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        public float MoveAcceleration { get; private set; } = 100f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _damageValue = 1;

        public enum EnemyStates { Idle, Chase, Attack }
        public FiniteStateMachine<Enemy, EnemyStates> Fsm { get; private set; }
        public NavAgent2D Agent { get; private set; }
        public AnimationPlayer AnimPlayer { get; private set; }
        public Sprite Sprite { get; private set; }

        public Enemy Init()
        {
            AddToGroup("Enemy");
            Agent = GetNode<NavAgent2D>("NavAgent2D").Init();
            AnimPlayer = GetNode<AnimationPlayer>("NavAgent2D/AnimationPlayer");
            Sprite = GetNode<Sprite>("NavAgent2D/Sprite");
            Agent.Connect("BodyColliding", this, nameof(OnBodyColliding));
            Agent.Connect("ScreenEntered", this, nameof(OnScreenEnter));
            Agent.Connect("ScreenExited", this, nameof(OnScreenExit));
            Fsm = new FiniteStateMachine<Enemy, EnemyStates>();
            return this;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            foreach (var pair in Fsm.States) pair.Value.ExitTree();
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            StateController();
            Fsm.Process(delta);
            DirectionControl();
        }
        
        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            Fsm.PhysicsProcess(delta);
            Agent.Velocity = Agent.MoveInArea(Agent.Velocity, delta, Vector2.Up);
            if (!Agent.IsOnFloor()) Agent.Velocity.y += Gravity * delta;
            else if (!Agent.SnapDisabled) Agent.Velocity.y = Gravity * delta;
        }
        
        public void SetTarget(NavBody2D target) => Agent.TargetNavBody = target;
        
        public void PlayAnimation(string name, float? duration = null)
        {
            float speed = 1f;
            if (duration != null) speed = AnimPlayer.GetAnimation(name).Length / duration.Value;
            AnimPlayer.PlaybackSpeed = speed;
            AnimPlayer.Play(name);
        }
        
        protected abstract void StateController();

        protected void OnScreenEnter()
        {
            App.SetNodeActive(this, true);
        }

        protected void OnScreenExit()
        {
            App.SetNodeActive(this, false);
        }

        private void OnBodyColliding(Node body)
        {
            if (!(body is NavBody2D targetNavBody)) return;
            if (targetNavBody.IsUnhurtable) return;
            Events.Singleton.EmitSignal(
                nameof(Events.Damaged), targetNavBody, _damageValue, Agent,
                Agent.GlobalPosition.DirectionTo(targetNavBody.GlobalPosition)
            );
        }
        
        private void DirectionControl()
        {
            switch (Agent.Direction.x)
            {
                case 1:
                    Sprite.FlipH = false;
                    break;
                case -1:
                    Sprite.FlipH = true;
                    break;
            }
        }
    }
}
