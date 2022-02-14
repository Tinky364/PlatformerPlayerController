using System.Threading;
using System.Threading.Tasks;
using Godot;
using Manager;
using NavTool;

namespace UI
{
    public class GameSceneGui : CanvasLayer
    {
        private Label _coinCountLabel;
        private TextureProgress _healthProgress;
        private Control _pausePanel;
        private Control _hud;

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

        private CancellationTokenSource _cancellationSource;
        
        public override void _Ready()
        {
            _hud = GetNode<Control>(_hudPath);
            _coinCountLabel = GetNode<Label>(_coinCountLabelPath);
            _healthProgress = GetNode<TextureProgress>(_healthProgressPath);
            _pausePanel = GetNode<Control>(_pausePanelPath);
            
            _pausePanel.Visible = false;
            
            Events.Singleton.Connect("CoinCountChanged", this, nameof(OnCoinCountChanged));
            Events.Singleton.Connect("PlayerHealthChanged", this, nameof(OnHealthChanged));
            Events.Singleton.Connect("PlayerDied", this, nameof(OnPlayerDied));
        }

        private async void OnPlayerDied()
        {
            if (GameManager.Singleton.UiState == GameManager.GameState.Pause) return;

            GameManager.Singleton.GuiDisableInput(true);
            await ToSignal(GetTree().CreateTimer(1f), "timeout");
            await FadeControlAlpha(_hud, 1f, 0f, 1f);
            await FadeControlAlpha(_pausePanel, 0f, 1f, _pausePanelOpenDur);
            GameManager.Singleton.GuiDisableInput(false);
        }
        
        private void OnCoinCountChanged(int newCount)
        {
            if (GameManager.Singleton.UiState == GameManager.GameState.Pause) return;
            
            _coinCountLabel.Text = newCount.ToString();
        }

        private void OnHealthChanged(int newHealth, int maxHealth, NavBody2D attacker)
        {
            if (GameManager.Singleton.UiState == GameManager.GameState.Pause) return;

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
                if (token.IsCancellationRequested) return;
                float t = count / _healthProgressDur;
                t = 1 - Mathf.Pow(1 - t, 3);
                _healthProgress.Value = Mathf.Lerp(from, to, t);
                count += GetProcessDeltaTime();
                await ToSignal(GetTree(), "idle_frame");
            }
            _healthProgress.Value = to;
        }
        
        private async Task FadeControlAlpha(Control control, float from, float to, float duration)
        {
            control.Visible = true;
            float count = 0f;
            while (count < duration)
            {
                float alpha = Mathf.Lerp(from, to, count / duration);
                control.Modulate = new Color(
                    control.Modulate.r, control.Modulate.g, control.Modulate.b, alpha
                );
                count += GetProcessDeltaTime();
                await ToSignal(GetTree(), "idle_frame");
            }
            control.Modulate = new Color(control.Modulate.r, control.Modulate.g, control.Modulate.b, to);
            if (to == 0) control.Visible = false;
        }
    }
}
