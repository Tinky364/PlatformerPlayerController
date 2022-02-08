using Godot;

namespace StateMachine
{
    public abstract class State<T> : Resource
    {
        public T Id { get; private set; }

        protected void Initialize(T id) => Id = id;
        public abstract void Enter();
        public abstract void Exit();
        public abstract void Process(float delta);
        public abstract void PhysicsProcess(float delta);
    }
}