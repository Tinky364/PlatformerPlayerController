using Godot;
using System;

public class GameManager : Node
{
    [Export] private PackedScene _startingScenePath;
    private Viewport _root;
    private ResourceInteractiveLoader _loader;
    private Node _currentScene;
    private int _waitFrames;
    private ulong _timeMax = 100;
    
    public override void _Ready()
    {
        _root = GetTree().Root;
        _currentScene = _root.GetChild(_root.GetChildCount() - 1);
    }

    public override void _Process(float delta)
    {
        if (_loader is null)
        {
            SetProcess(false);
            return;
        }

        if (_waitFrames > 0)
        {
            _waitFrames -= 1;
            return;
        }

        ulong t = OS.GetTicksMsec();
        while (OS.GetTicksMsec() < t + _timeMax)
        {
            Error err = _loader.Poll();

            if (err == Error.FileEof)
            {
                PackedScene resource = (PackedScene) _loader.GetResource();
                _loader = null;
                SetNewScene(resource);
                break;
            }
            else if (err == Error.Ok)
            {
                UpdateProgress();
            }
            else
            {
                GD.PrintErr("Poll error!");
                _loader = null;
                break;
            }
        }
    }

    private void UpdateProgress()
    {
        float progress = (float)_loader.GetStage() / _loader.GetStageCount();
    }

    private void SetNewScene(PackedScene sceneResource)
    {
        _currentScene = sceneResource.Instance();
        GetNode("/root").AddChild(_currentScene);
    }

    private void GoToScene(string path)
    {
        _loader = ResourceLoader.LoadInteractive(path);
        if (_loader is null)
        {
            GD.PrintErr("ResourceInteractiveLoader is null!");
            return;
        }
        SetProcess(true);
        _currentScene.QueueFree();
        _waitFrames = 1;
    }

    public void StartGame()
    {
        GD.Print("GameScene loading!");
        GoToScene(_startingScenePath.ResourcePath);
    }
}
