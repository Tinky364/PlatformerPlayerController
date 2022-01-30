using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class StateMachine<T> : Reference
    {
        protected readonly Dictionary<T, State<T>> States;
        
        private State<T> _currentState;
        public State<T> CurrentState => _currentState;

        public bool IsStateLocked { get; set; }

        public StateMachine()
        {
            States = new Dictionary<T, State<T>>();
            IsStateLocked = false;
        }

        public void AddState(State<T> state)
        {
            if (States.ContainsKey(state.Id)) return;
            States.Add(state.Id, state);
        }
        
        public State<T> GetState(T stateId) => States.ContainsKey(stateId) ? States[stateId] : null;
        
        public void SetCurrentState(T stateId)
        {
            State<T> state = States[stateId];
            SetCurrentState(state);
        }
        
        public void SetCurrentState(State<T> newState)
        {
            if (IsStateLocked) return;
            if (_currentState == newState) return;
            if (!States.ContainsKey(newState.Id)) return;
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }
        
        public void _Process(float delta)
        {
            _currentState?.Process(delta);
        }

        public void _PhysicsProcess(float delta)
        {
            _currentState?.PhysicsProcess(delta);
        }
    }
}
