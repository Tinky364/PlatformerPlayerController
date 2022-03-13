using Godot;

namespace AI
{
    public abstract class State<TOwner, TKey> : Resource
    {
        public TOwner Owner { get; private set; }
        
        public TKey Key { get; private set; }

        public abstract void Enter();
        
        public abstract void Process(float delta);
        
        public abstract void PhysicsProcess(float delta);
        
        public abstract void Exit();

        public abstract void ExitTree();

        public virtual void Initialize(TOwner owner, TKey key)
        {
            Owner = owner;
            Key = key;
        }
    }
}