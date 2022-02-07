using System.Threading.Tasks;
using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class GameManager : Node
    {
        private static GameManager _singleton;
        public static GameManager Singleton => _singleton;

        public SceneTree Tree => GetTree();
        private Viewport Root => Tree.Root;
        private Node _currentScene;
        
        public enum GameState { Play, Pause }
        private GameState _currentGameState;
        public GameState CurrentGameState
        {
            get => _currentGameState;
            set
            {
                switch (value)
                {
                    case GameState.Play:
                        Tree.Paused = false;
                        break;
                    case GameState.Pause:
                        Tree.Paused = true;
                        break;
                }
                _currentGameState = value;
            }
        } 

        public override void _EnterTree()
        {
            if (_singleton == null) _singleton = this;
            else GD.Print($"Multiple instances of singleton class named {Name}!");
        }

        public override void _Ready()
        {
            _currentScene = Root.GetChild(Root.GetChildCount() - 1);
            
            PauseMode = PauseModeEnum.Process;
            _currentScene.PauseMode = PauseModeEnum.Stop;
        }

        public async void LoadScene(string path)
        {
            CurrentGameState = GameState.Pause;
            
            PackedScene scene = await LoadAsync<PackedScene>(path);
            _currentScene?.QueueFree();
            _currentScene = scene.Instance();
            Root.AddChild(_currentScene);
            
            CurrentGameState = GameState.Play;
        }
        
        public void QuitGame()
        {
            Tree.Quit();
        }

        public async Task<T> LoadAsync<T>(string path) where T : Resource
        {
            using (var loader = ResourceLoader.LoadInteractive(path))
            {
                GD.Print($"Resource Load started -> {path}");
                Error err;
                do 
                {
                    err = loader.Poll();
                    await ToSignal(Tree, "idle_frame");
                } while (err == Error.Ok);

                if (err != Error.FileEof)
                    GD.PrintErr("Poll error!");

                GD.Print($"Resource Load ended -> {path}");
                return (T) loader.GetResource();
            }
        }
    }
    
    
}
