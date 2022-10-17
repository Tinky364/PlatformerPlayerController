using System.Threading;
using Godot;
using Game.Level.PlayerStateMachine;
using Game.Service;
using NavTool;

namespace Game.Interface
{
    public class Hud : Panel
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
        
        public Hud Init(Player player)
        {
            base.Init();
            _coinCountLabel = GetNode<Label>(_coinCountLabelPath);
            _healthProgress = GetNode<TextureProgress>(_healthProgressPath);
            player.Connect(nameof(Player.CoinCountChanged), this, nameof(OnCoinCountChanged));
            player.Connect(nameof(Player.HealthChanged), this, nameof(OnHealthChanged));
            player.Connect(nameof(Player.Died), this, nameof(OnPlayerDied));
            return this;
        }

        private async void OnPlayerDied()
        {
            await TreeTimer.Singleton.Wait(1f);
            App.SetNodeActive(this, false);
        }
        
        private void OnCoinCountChanged(int coinCount)
        {
            if (App.Singleton.InterfaceState == App.GameState.Pause) return;
            _coinCountLabel.Text = coinCount.ToString();
        }

        private void OnHealthChanged(int newHealth, int maxHealth, NavBody2D attacker)
        {
            if (App.Singleton.InterfaceState == App.GameState.Pause) return;
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
                await TreeTimer.Singleton.Wait(GetProcessDeltaTime());
            }
            _healthProgress.Value = to;
        }
    }
}
