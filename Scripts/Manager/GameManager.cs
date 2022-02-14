using System.Threading.Tasks;
using Godot;

namespace Manager
{
    public class GameManager : Node
    {
        private static GameManager _singleton;
        public static GameManager Singleton => _singleton;

        public SceneTree Tree => GetTree();
        private Viewport Root => Tree.Root;
        private Node _currentScene;
        private CanvasLayer _world;
        private CanvasLayer _gui;

        public enum GameState { Play, Pause }

        private GameState _worldState;
        public GameState WorldState => _worldState;
        private GameState _uiState;
        public GameState UiState => _uiState;

        public override void _EnterTree()
        {
            if (_singleton == null)
                _singleton = this;
            else
                GD.Print($"Multiple instances of singleton class named {Name}!");
        }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            Events.Singleton.PauseMode = PauseModeEnum.Process;

            SetCurrentScene(Root.GetChild(Root.GetChildCount() - 1));
        }

        public async void LoadScene(string path)
        {
            SetGameState(GameState.Pause, GameState.Pause);

            PackedScene scene = await LoadAsync<PackedScene>(path);
            _currentScene?.QueueFree();
            SetCurrentScene(scene.Instance());

            SetGameState(GameState.Play, GameState.Play);
        }

        public void SetGameState(GameState worldState, GameState uiState)
        {
            switch (worldState)
            {
                case GameState.Play:
                    _world.PauseMode = PauseModeEnum.Process;
                    break;
                case GameState.Pause:
                    _world.PauseMode = PauseModeEnum.Stop;
                    break;
            }

            _worldState = worldState;
            switch (uiState)
            {
                case GameState.Play:
                    _gui.PauseMode = PauseModeEnum.Process;
                    Root.GuiDisableInput = false;
                    break;
                case GameState.Pause:
                    _gui.PauseMode = PauseModeEnum.Stop;
                    Root.GuiDisableInput = true;
                    break;
            }

            _uiState = uiState;

            if (worldState == GameState.Pause || uiState == GameState.Pause)
            {
                Tree.Paused = true;
                Physics2DServer.SetActive(true);
            }
            else
            {
                Tree.Paused = false;
            }
        }

        public void GuiDisableInput(bool value)
        {
            Root.GuiDisableInput = value;
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

                if (err != Error.FileEof) GD.PrintErr("Poll error!");

                GD.Print($"Resource Load ended -> {path}");
                return (T) loader.GetResource();
            }
        }

        private void SetCurrentScene(Node scene)
        {
            _currentScene = scene;
            if (!_currentScene.IsInsideTree()) Root.AddChild(_currentScene);
            _world = _currentScene.GetNode<CanvasLayer>("World");
            _gui = _currentScene.GetNode<CanvasLayer>("Gui");

            _currentScene.PauseMode = PauseModeEnum.Process;
            _world.PauseMode = PauseModeEnum.Stop;
            _gui.PauseMode = PauseModeEnum.Stop;
        }
    }
}
