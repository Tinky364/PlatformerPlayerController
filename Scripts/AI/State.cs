using Godot;

namespace AI
{
    public abstract class State<TOwner, TKey> : Resource
    {
        protected TOwner Owner { get; private set; }
        
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
            if (!(owner is Node node)) return;
            if (node.Get("Fsm") is FiniteStateMachine<TOwner, TKey> fsm) fsm.AddState(this);
            else GD.PushError("Finite State Machine variable name is not Fsm!");
        }
    }
}