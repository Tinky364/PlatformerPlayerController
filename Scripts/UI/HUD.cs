using Godot;

namespace PlatformerPlayerController.Scripts.UI
{
    public class HUD : CanvasLayer
    {
        [Export]
        private NodePath _coinCountLabelPath = default;
    
        private Label _coinCountLabel;
    
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
