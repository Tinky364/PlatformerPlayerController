using System;
using Godot;

namespace Game.Interface
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
                    App.Singleton.LoadSceneAsync(LoadScenePath);
                    break;
                case ButtonTypes.QuitGameButton:
                    App.Singleton.QuitGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
