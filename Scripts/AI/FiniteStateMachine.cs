using Godot;
using Godot.Collections;

namespace AI
{
    public class FiniteStateMachine<TOwner, TKey> : Reference
    {
        public Dictionary<TKey, State<TOwner, TKey>> States { get; }
        public State<TOwner, TKey> PreviousState { get; private set; }
        public State<TOwner, TKey> CurrentState { get; private set; }
        public bool IsStateLocked { get; private set; }

        public FiniteStateMachine()
        {
            States = new Dictionary<TKey, State<TOwner, TKey>>();
            IsStateLocked = false;
        }

        public void AddState(State<TOwner, TKey> state)
        {
            if (States.ContainsKey(state.Key)) return;
            States.Add(state.Key, state);
        }

        public State<TOwner, TKey> GetState(TKey stateKey) => 
            States.ContainsKey(stateKey) ? States[stateKey] : null;
        
        public void SetCurrentState(TKey stateKey, bool isStateLocked = false)
        {
            if (IsStateLocked || !States.ContainsKey(stateKey)) return;
            if (CurrentState == States[stateKey]) return;
            CurrentState?.Exit();
            PreviousState = CurrentState;
            IsStateLocked = isStateLocked;
            CurrentState = States[stateKey];
            CurrentState?.Enter();
        }
        
        public void StopCurrentState()
        {
            IsStateLocked = false;
            CurrentState?.Exit();
            CurrentState = null;
        }

        public void Process(float delta) => CurrentState?.Process(delta);

        public void PhysicsProcess(float delta) => CurrentState?.PhysicsProcess(delta);
    }
}
