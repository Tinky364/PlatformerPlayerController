using Godot;
using System.Threading.Tasks;

public class GameManager : Node
{
    private static Node _currentScene;
    private static Viewport _root;
    private static SignalAwaiter _signal;
    
    public override void _Ready()
    {
        _signal = ToSignal(GetTree(), "idle_frame");
        _root = GetTree().Root;
        _currentScene = _root.GetChild(_root.GetChildCount() - 1);
    }

    public static async Task LoadScene(string path)
    {
        GD.Print($"Loading Scene: {path}");
        _currentScene?.QueueFree();
        PackedScene scene = await LoadAsync<PackedScene>(path);
        _currentScene = scene.Instance();
        _root.AddChild(_currentScene);
        GD.Print("Loading Scene Done.");
    }

    public static async Task<T> LoadAsync<T>(string path) where T : Resource
    {
        using (var loader = ResourceLoader.LoadInteractive(path))
        {
            Error err;
            do 
            {
                err = loader.Poll();
                await _signal;
            } while (err == Error.Ok);

            if (err != Error.FileEof)
                GD.PrintErr("Poll error!");

            return (T) loader.GetResource();
        }
    }
}
