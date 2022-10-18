using CustomRegister;
using Game.Fsm;
using Game.Service.Debug;

namespace Game.Level.Players.States
{
    [Register]
    public class PlayerStateDead : State<Player, Player.PlayerStates>
    {
        public override async void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            Owner.IsUnhurtable = true;
            App.SetNodeActive(Owner, false);
        }
       
        public override void Process(float delta) { }
        public override void PhysicsProcess(float delta) { }
        public override void Exit() { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }
    }
}

