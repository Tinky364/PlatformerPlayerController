using System;
using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class ButtonController : Button, IButtonType
    {
        [Export]
        public ButtonTypes ButtonType { get; set; }
        [Export(PropertyHint.File, "*.tscn")]
        public string LoadScenePath { get; set; }

        public override void _Ready()
        {
            Connect("pressed", this, nameof(OnPressed));
        }

        public void OnPressed()
        {
            switch (ButtonType)
            {
                case ButtonTypes.ChangeSceneButton:
                    GameManager.LoadScene(LoadScenePath);
                    break;
                case ButtonTypes.QuitGameButton:
                    GameManager.QuitGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
