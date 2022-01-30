using System;
using Godot;

namespace PlatformerPlayerController.Scripts.StateMachine
{
    public abstract class State<T> : Reference
    {
        public T Id { get; }

        protected State() {}

        public State(T id) => Id = id;

        public abstract void Enter();
        public abstract void Exit();
        public abstract void Process(float delta);
        public abstract void PhysicsProcess(float delta);
    }
}