using System.Threading;
using System.Threading.Tasks;
using Godot;
using Manager;
using NavTool;

namespace UI
{
    public class TestSceneGui : CanvasLayer
    {
        [Export]
        private NodePath _hudPath = default;
        [Export]
        private NodePath _coinCountLabelPath = default;
        [Export]
        private NodePath _healthProgressPath = default;
        [Export]
        private float _healthProgressDur = 2f;
        [Export]
        private NodePath _pausePanelPath = default;
        [Export]
        private float _pausePanelOpenDur = 3f;

        private Label _coinCountLabel;
        private TextureProgress _healthProgress;
        private Control _pausePanel;
        private Control _hud;
        
        private CancellationTokenSource _cancellationSource;
        
        public override void _Ready()
        {
            _hud = GetNode<Control>(_hudPath);
            _coinCountLabel = GetNode<Label>(_coinCountLabelPath);
            _healthProgress = GetNode<TextureProgress>(_healthProgressPath);
            _pausePanel = GetNode<Control>(_pausePanelPath);
            _pausePanel.Visible = false;
            Events.S.Connect("PlayerCoinCountChanged", this, nameof(OnCoinCountChanged));
            Events.S.Connect("PlayerHealthChanged", this, nameof(OnHealthChanged));
            Events.S.Connect("PlayerDied", this, nameof(OnPlayerDied));
        }

        private async void OnPlayerDied()
        {
            if (GM.S.UiState == GM.GameState.Pause) return;
            GM.S.GuiDisableInput(true);
            await TreeTimer.S.Wait(1f);
            await FadeControlAlpha(_hud, 1f, 0f, 1f);
            await FadeControlAlpha(_pausePanel, 0f, 1f, _pausePanelOpenDur);
            GM.S.SetGameState(GM.GameState.Pause, GM.GameState.Play);
            GM.S.GuiDisableInput(false);
        }
        
        private void OnCoinCountChanged(int coinCount)
        {
            if (GM.S.UiState == GM.GameState.Pause) return;
            _coinCountLabel.Text = coinCount.ToString();
        }

        private void OnHealthChanged(int newHealth, int maxHealth, NavBody2D attacker)
        {
            if (GM.S.UiState == GM.GameState.Pause) return;
            int targetHealth = (int)_healthProgress.MaxValue * newHealth / maxHealth;
            _cancellationSource?.Cancel();
            _cancellationSource = new CancellationTokenSource();
            HealthLerp(targetHealth, _cancellationSource.Token);
        }

        private async void HealthLerp(int newHealth, CancellationToken token)
        {
            float from = (float)_healthProgress.Value;
            float to = newHealth;
            float count = 0f;
            while (count < _healthProgressDur)
            {
                if (!IsInstanceValid(this)) return;
                if (token.IsCancellationRequested) return;
                float t = count / _healthProgressDur;
                t = 1 - Mathf.Pow(1 - t, 3);
                _healthProgress.Value = Mathf.Lerp(from, to, t);
                count += GetProcessDeltaTime();
                await TreeTimer.S.Wait(GetProcessDeltaTime());
            }
            _healthProgress.Value = to;
        }

        private async Task FadeControlAlpha(
            CanvasItem control, float from, float to, float duration)
        {
            control.Visible = true;
            float count = 0f;
            while (count < duration)
            {
                if (!IsInstanceValid(this)) return;
                float alpha = Mathf.Lerp(from, to, count / duration);
                control.Modulate = new Color(
                    control.Modulate.r, control.Modulate.g, control.Modulate.b, alpha
                );
                count += GetProcessDeltaTime();
                await TreeTimer.S.Wait(GetProcessDeltaTime());
            }
            control.Modulate = new Color(
                control.Modulate.r, control.Modulate.g, control.Modulate.b, to
            );
            if (to == 0) control.Visible = false;
        }
    }
}
