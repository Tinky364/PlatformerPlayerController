using AI;

namespace PlayerStateMachine
{
    public class HangState : State<Player.PlayerStates>
    {
        public void Initialize(Player player) { }
        
        public override void Enter() { }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta) { }

        public override void Exit() { }
    }
}
