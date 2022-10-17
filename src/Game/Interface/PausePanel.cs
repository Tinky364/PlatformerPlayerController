using Godot;
using Game.Level.PlayerStateMachine;
using Game.Service;
using Game.Service.Debug;

namespace Game.Interface
{
    public class PausePanel : Panel
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _pausePanelOpenDur = 0.5f;

        private bool _isPausePanelOpen;
        private bool _lockPausePanel;

        public PausePanel Init(Player player)
        {
            base.Init();
            App.SetNodeActive(this, false);
            player.Connect(nameof(Player.Died), this, nameof(OnPlayerDied));
            return this;
        }

        private void OnPlayerDied() => _lockPausePanel = true;

        public void PausePanelControl()
        {
            if (_lockPausePanel) return;
            if (_isPausePanelOpen) ClosePausePanel();
            else OpenPausePanel();
        }

        private void OpenPausePanel()
        {
            _isPausePanelOpen = true;
            App.SetNodeActive(this, true);
            App.Singleton.SetGameState(App.GameState.Pause, App.GameState.Play);
            FocusControl.GrabFocus();
        }
        
        private void ClosePausePanel()
        {
            _isPausePanelOpen = false;
            App.SetNodeActive(this, false);
            App.Singleton.SetGameState(App.GameState.Play, App.GameState.Play);
        }
    }
}
