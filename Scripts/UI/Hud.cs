using System.Threading;
using Godot;
using Manager;
using NavTool;

namespace UI
{
    public class Hud : Panel<TestSceneGui>
    {
        [Export]
        private NodePath _coinCountLabelPath = default;
        [Export]
        private NodePath _healthProgressPath = default;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _healthProgressDur = 2f;
        
        private Label _coinCountLabel;
        private TextureProgress _healthProgress;
        
        private CancellationTokenSource _cancellationSource;
        
        public override void _Ready()
        {
            base._Ready();
            _coinCountLabel = GetNode<Label>(_coinCountLabelPath);
            _healthProgress = GetNode<TextureProgress>(_healthProgressPath);
            Events.S.Connect("PlayerCoinCountChanged", this, nameof(OnCoinCountChanged));
            Events.S.Connect("PlayerHealthChanged", this, nameof(OnHealthChanged));
            Events.S.Connect("PlayerDied", this, nameof(OnPlayerDied));
        }

        private async void OnPlayerDied()
        {
            await TreeTimer.S.Wait(1f);
            await Gui.FadeControlAlpha(this, 1f, 0f, 1f);
            GM.SetNodeActive(this, false);
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
    }
}
