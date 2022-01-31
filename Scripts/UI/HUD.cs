using Godot;

namespace PlatformerPlayerController.Scripts.UI
{
    public class HUD : CanvasLayer
    {
        private Label _coinCountLabel;

        [Export]
        private NodePath _coinCountLabelPath = default;
    
        public override void _Ready()
        {
            _coinCountLabel = GetNode<Label>(_coinCountLabelPath);
        }

        private void OnPlayerCoinCountChanged(int count)
        {
            _coinCountLabel.Text = count.ToString();
        }
    }
}
