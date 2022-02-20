using Godot;
using System;
using AI;

namespace PlayerStateMachine
{
    public class HangState : State<Player.PlayerStates>
    {
        public void Initialize(Player player)
        {
        }
        
        public override void Enter() { throw new NotImplementedException(); }

        public override void Process(float delta) { throw new NotImplementedException(); }

        public override void PhysicsProcess(float delta) { throw new NotImplementedException(); }

        public override void Exit() { throw new NotImplementedException(); }
    }
}
