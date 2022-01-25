using Godot;
using System;

public class ButtonController : Button
{
    [Export]
    private ButtonTypes _buttonType;
    [Export(PropertyHint.File, "*.tscn")]
    private string _loadScenePath;

    public override void _Ready()
    {
        Connect("pressed", this, "OnPressed");
    }

    private void OnPressed()
    {
        if (_buttonType == ButtonTypes.ChangeSceneButton)
            GameManager.LoadScene(_loadScenePath);
        else if (_buttonType == ButtonTypes.QuitGameButton)
            GameManager.QuitGame();
    }
    
    private enum ButtonTypes
    {
        ChangeSceneButton,
        QuitGameButton
    }
}
