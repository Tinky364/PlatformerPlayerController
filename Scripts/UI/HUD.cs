using System.Threading;
using Godot;

namespace UI
{
    public class Hud : CanvasLayer
    {
        private Label _coinCountLabel;
        private TextureProgress _healthProgress;

        [Export]
        private NodePath _coinCountLabelPath = default;
        [Export]
        private NodePath _healthProgressPath = default;
        [Export]
        private float _healthProgressDur = 2f;

        private CancellationTokenSource _cancellationTokenSource;
        
        public override void _Ready()
        {
            _coinCountLabel = GetNode<Label>(_coinCountLabelPath);
            _healthProgress = GetNode<TextureProgress>(_healthProgressPath);
            
            Events.Singleton.Connect("CoinCountChanged", this, nameof(OnCoinCountChanged));
            Events.Singleton.Connect("HealthChanged", this, nameof(OnHealthChanged));
        }

        private void OnCoinCountChanged(int newCount)
        {
            _coinCountLabel.Text = newCount.ToString();
        }

        private void OnHealthChanged(int newHealth, int maxHealth)
        {
            int targetHealth = (int)_healthProgress.MaxValue * newHealth / maxHealth;
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            HealthLerp(targetHealth, _cancellationTokenSource.Token);
        }

        private async void HealthLerp(int newHealth, CancellationToken cancellationToken)
        {
            float from = (float)_healthProgress.Value;
            float to = newHealth;

            float count = 0f;
            while (count < _healthProgressDur)
            {
                if (cancellationToken.IsCancellationRequested) return;

                float t = count / _healthProgressDur;
                t = 1 - Mathf.Pow(1 - t, 3);
                _healthProgress.Value = Mathf.Lerp(from, to, t);
                count += GetProcessDeltaTime();
                await ToSignal(GetTree(), "idle_frame");
            }
            _healthProgress.Value = to;
            _cancellationTokenSource = null;
        }
    }
}
