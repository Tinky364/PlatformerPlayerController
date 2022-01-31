using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class StateMachine<T> : Reference
    {
        public State<T> CurrentState { get; private set; }
        
        protected readonly Dictionary<T, State<T>> States;
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
            if (CurrentState == newState) return;
            if (IsStateLocked) return;
            if (!States.ContainsKey(newState.Id)) return;
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
        }
        
        public void _Process(float delta)
        {
            CurrentState?.Process(delta);
        }

        public void _PhysicsProcess(float delta)
        {
            CurrentState?.PhysicsProcess(delta);
        }
    }
}
