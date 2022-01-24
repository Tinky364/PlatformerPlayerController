using Godot;
using System;

public class ButtonSceneLoader : Button
{
    [Export(PropertyHint.File, "*.tscn")] private string _loadScenePath;

    public override void _Ready()
    {
        Connect("pressed", this, "OnPressed");
    }

    private void OnPressed()
    {
        GameManager.LoadScene(_loadScenePath);
    }
}
