using Godot;
using Manager;

namespace UI
{
    public class GameOverPanel : Panel<TestSceneGui>
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _gameOverPanelOpenDur = 3f;

        public override void _Ready()
        {
            base._Ready();
            Events.S.Connect("PlayerDied", this, nameof(OnPlayerDied));
            GM.SetNodeActive(this, false);
        }
        
        private async void OnPlayerDied()
        {
            if (GM.S.UiState == GM.GameState.Pause) return;
            InputManager.S.LockAllInputs(true);
            await TreeTimer.S.Wait(2f);
            GM.SetNodeActive(this, true);
            await Gui.FadeControlAlpha(this, 0f, 1f, _gameOverPanelOpenDur);
            GM.S.SetGameState(GM.GameState.Pause, GM.GameState.Play);
            InputManager.S.LockAllInputs(false);
            FocusControl.GrabFocus();
        }
    }
}