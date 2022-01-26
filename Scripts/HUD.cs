using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class HUD : CanvasLayer
    {
        [Export]
        private NodePath _coinCountLabelPath;
    
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
