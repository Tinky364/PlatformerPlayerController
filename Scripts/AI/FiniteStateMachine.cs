using Godot;
using Godot.Collections;

namespace AI
{
    public class FiniteStateMachine<TOwner, TKey> : Reference
    {
        public State<TOwner, TKey> PreviousState { get; private set; }
        public State<TOwner, TKey> CurrentState { get; private set; }
        public bool IsStateLocked { get; private set; }
        private readonly Dictionary<TKey, State<TOwner, TKey>> _states;

        public FiniteStateMachine()
        {
            _states = new Dictionary<TKey, State<TOwner, TKey>>();
            IsStateLocked = false;
        }

        public void AddState(State<TOwner, TKey> state)
        {
            if (_states.ContainsKey(state.Key)) return;
            _states.Add(state.Key, state);
        }

        public State<TOwner, TKey> GetState(TKey stateKey) => 
            _states.ContainsKey(stateKey) ? _states[stateKey] : null;
        
        public void SetCurrentState(TKey stateKey, bool isStateLocked = false)
        {
            if (IsStateLocked || !_states.ContainsKey(stateKey)) return;
            if (CurrentState == _states[stateKey]) return;
            CurrentState?.Exit();
            PreviousState = CurrentState;
            IsStateLocked = isStateLocked;
            CurrentState = _states[stateKey];
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
