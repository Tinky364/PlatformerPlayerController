using Godot;

namespace AI
{
    public abstract class State<T> : Resource
    {
        public T Key { get; private set; }

        public abstract void Enter();
        
        public abstract void Process(float delta);
        
        public abstract void PhysicsProcess(float delta);
        
        public abstract void Exit();
        
        protected void Initialize(T key) => Key = key;
    }
}