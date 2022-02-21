using Godot;
using Godot.Collections;

namespace AI
{
    public class StateMachine<T> : Reference
    {
        public State<T> PreviousState { get; private set; }
        public State<T> CurrentState { get; private set; }
        public bool IsStateLocked { get; private set; }
        private readonly Dictionary<T, State<T>> _states;

        public StateMachine()
        {
            _states = new Dictionary<T, State<T>>();
            IsStateLocked = false;
        }

        public void AddState(State<T> state)
        {
            if (_states.ContainsKey(state.Key)) return;
            _states.Add(state.Key, state);
        }

        public State<T> GetState(T stateKey) => 
            _states.ContainsKey(stateKey) ? _states[stateKey] : null;
        
        public void SetCurrentState(T stateKey, bool isStateLocked = false)
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

        public void _Process(float delta) => CurrentState?.Process(delta);

        public void _PhysicsProcess(float delta) => CurrentState?.PhysicsProcess(delta);
    }
}
