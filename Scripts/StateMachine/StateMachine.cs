using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public class StateMachine : KinematicBody2D
    {
        private State _currentState;
        
        public override void _Ready()
        {
        }

        public override void _Process(float delta)
        {
            _currentState.Process(delta);
        }

        public override void _PhysicsProcess(float delta)
        {
            _currentState.PhysicsProcess(delta);
        }

        public void ChangeState(State newState)
        {
            _currentState.Exit();
            _currentState = newState;
            _currentState.Enter();
        }
    }
}
