using System;
using Godot;
using Manager;

namespace UI
{
    public class ButtonController : Button, IButtonType
    {
        [Export]
        public ButtonTypes ButtonType { get; set; }
        [Export(PropertyHint.File, "*.tscn")]
        public string LoadScenePath { get; set; }
        [Export]
        private bool _setFocus;

        public override void _Ready()
        {
            Connect("pressed", this, nameof(OnPressed));

            if (_setFocus)
            {
                Events.S.Connect("PlayerDied", this, nameof(OnPlayerDied));
                GrabFocus();
            }
        }

        private void OnPlayerDied()
        {
            if (_setFocus) GrabFocus();
        }

        public void OnPressed()
        {
            switch (ButtonType)
            {
                case ButtonTypes.ChangeSceneButton: 
                    GM.S.LoadScene(LoadScenePath);
                    break;
                case ButtonTypes.QuitGameButton:
                    GM.S.QuitGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
