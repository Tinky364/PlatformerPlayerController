using System;
using Godot;
using Manager;

namespace UI
{
    public class TextureButtonController : TextureButton, IButtonType
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
                    GameManager.Singleton.LoadScene(LoadScenePath);
                    break;
                case ButtonTypes.QuitGameButton:
                    GameManager.Singleton.QuitGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
