using AI;
using Manager;

namespace PlayerStateMachine
{
    public class DeadState : State<Player.PlayerStates>
    {
        private Player P { get; set; }

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Dead);
            P = player;
            P.Fsm.AddState(this);
        }

        public override async void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.IsUnhurtable = true;
            await TreeTimer.S.Wait(2f);
            GM.SetNodeActive(P, false);
        }
       
        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta) { }

        public override void Exit() { }
    }
}

