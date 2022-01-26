using System.Threading.Tasks;
using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class GameManager : Node
    {
        private static SceneTree _tree;
        private static Viewport _root;
        private static Node _currentScene;
        private static SignalAwaiter _endOfFrameSignal;
    
        public override void _Ready()
        {
            _tree = GetTree();
            _root = _tree.Root;
            _currentScene = _root.GetChild(_root.GetChildCount() - 1);
            _endOfFrameSignal = ToSignal(_tree, "idle_frame");
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
                    await _endOfFrameSignal;
                } while (err == Error.Ok);

                if (err != Error.FileEof)
                    GD.PrintErr("Poll error!");

                return (T) loader.GetResource();
            }
        }

        public static void QuitGame()
        {
            _tree.Quit();
        }
    }
}
