using Game.Level.PlayerStateMachine;
using Game.Service;

namespace Game.Interface
{
    public class GameOverPanel : Panel
    {
        public GameOverPanel Init(Player player)
        {
            base.Init();
            player.Connect(nameof(Player.Died), this, nameof(OnPlayerDied));
            App.SetNodeActive(this, false);
            return this;
        }
        
        private async void OnPlayerDied()
        {
            if (App.Singleton.InterfaceState == App.GameState.Pause) return;
            InputInvoker.Singleton.LockAllInputs(true);
            await TreeTimer.Singleton.Wait(2f);
            App.SetNodeActive(this, true);
            App.Singleton.SetGameState(App.GameState.Pause, App.GameState.Play);
            InputInvoker.Singleton.LockAllInputs(false);
            FocusControl.GrabFocus();
        }
    }
}