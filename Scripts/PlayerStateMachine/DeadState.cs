using AI;
using Manager;

namespace PlayerStateMachine
{
    public class DeadState : State<Player, Player.PlayerStates>
    {
        public override void Initialize(Player owner, Player.PlayerStates key)
        {
            base.Initialize(owner, key);
            Owner.Fsm.AddState(this);
        }

        public override async void Enter()
        {
            GM.Print(Owner.DebugEnabled, $"{Owner.Name}: {Key}");
            Owner.IsUnhurtable = true;
            await TreeTimer.S.Wait(2f);
            GM.SetNodeActive(Owner, false);
        }
       
        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta) { }

        public override void Exit() { }
        public override void ExitTree() { }
    }
}

