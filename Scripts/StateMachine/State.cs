using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public abstract class State : Resource
    {
        public StateMachine Machine { get; set; }

        public State()
        {
            
        }
        public State(StateMachine machine)
        {
            Machine = machine;
        }
        public abstract void Enter();
        public abstract void Exit();
        public abstract void Process(float delta);
        public abstract void PhysicsProcess(float delta);
    }
}