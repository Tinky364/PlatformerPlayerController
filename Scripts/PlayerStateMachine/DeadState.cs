using Godot;
using AI;

namespace PlayerStateMachine
{
    public class DeadState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        public override async void Enter()
        {
            if (P.DebugEnabled) GD.Print($"{P.Name}: {Key}");
        
            P.IsUnhurtable = true;
            await ToSignal(P.GetTree().CreateTimer(2f), "timeout");
            P.IsInactive = true;
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta) { }

        public override void Exit() { }
    
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Dead);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}

