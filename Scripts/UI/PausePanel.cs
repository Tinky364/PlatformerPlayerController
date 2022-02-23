using Godot;
using Manager;

namespace UI
{
    public class PausePanel : Panel<TestSceneGui>
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _pausePanelOpenDur = 0.5f;

        private bool _isPausePanelOpen;
        private bool _lockPausePanel;

        public override void _Ready()
        {
            base._Ready();
            GM.SetNodeActive(this, false);
            Gui.Connect("PausePanelRequested", this, nameof(PausePanelControl));
            Events.S.Connect("PlayerDied", this, nameof(OnPlayerDied));
        }

        private void OnPlayerDied() => _lockPausePanel = true;

        private void PausePanelControl()
        {
            if (_lockPausePanel) return;
            if (_isPausePanelOpen) ClosePausePanel();
            else OpenPausePanel();
        }

        private async void OpenPausePanel()
        {
            _isPausePanelOpen = true;
            GM.SetNodeActive(this, true);
            InputManager.S.LockAllInputs(true);
            GM.S.SetGameState(GM.GameState.Pause, GM.GameState.Play);
            await Gui.FadeControlAlpha(this, 0f, 1f, _pausePanelOpenDur);
            InputManager.S.LockAllInputs(false);
            FocusControl.GrabFocus();
        }
        
        private async void ClosePausePanel()
        {
            _isPausePanelOpen = false;
            GM.SetNodeActive(this, false);
            InputManager.S.LockAllInputs(true);
            GM.S.SetGameState(GM.GameState.Play, GM.GameState.Play);
            await Gui.FadeControlAlpha(this, 1f, 0f, 0.2f);
            InputManager.S.LockAllInputs(false);
        }
    }
}
