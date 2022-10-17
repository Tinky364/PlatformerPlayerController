using CustomRegister;
using Game.Fsm;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.PlayerStateMachine
{
    [Register]
    public class DeadState : State<Player, Player.PlayerStates>
    {
        public override async void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            Owner.IsUnhurtable = true;
            await TreeTimer.Singleton.Wait(2f);
            App.SetNodeActive(Owner, false);
        }
       
        public override void Process(float delta) { }
        public override void PhysicsProcess(float delta) { }
        public override void Exit() { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }
    }
}

