using System.Threading.Tasks;
using Godot;

namespace Manager
{
    public class GameManager : Node
    {
        private static GameManager _singleton;
        public static GameManager Singleton => _singleton;

        private SceneTree Tree => GetTree();
        private Viewport Root => Tree.Root;
        public SceneManager CurrentScene { get; private set; }

        public enum GameState { Play, Pause }

        private GameState _worldState;
        public GameState WorldState => _worldState;
        private GameState _uiState;
        public GameState UiState => _uiState;

        public override void _EnterTree()
        {
            if (_singleton == null) _singleton = this;
            else GD.Print($"Multiple instances of singleton class named {Name}!");
        }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            Events.Singleton.PauseMode = PauseModeEnum.Process;

            if (Root.GetChild(Root.GetChildCount() - 1) is SceneManager scene) SetCurrentScene(scene);
            else GD.PushWarning("First scene is not a SceneManager node!");
        }

        public async void LoadScene(string path)
        {
            SetGameState(GameState.Pause, GameState.Pause);

            PackedScene packedScene = await LoadAsync<PackedScene>(path);
            await UnloadCurrentScene();
            if (packedScene.Instance() is SceneManager scene) SetCurrentScene(scene);
            else GD.PushWarning("New loaded scene is not a SceneManager node!");

            SetGameState(GameState.Play, GameState.Play);
        }

        private async Task UnloadCurrentScene()
        {
            CurrentScene?.QueueFree();
            while (IsInstanceValid(CurrentScene))
                await ToSignal(Tree, "idle_frame");
        }

        public void SetGameState(GameState worldState, GameState uiState)
        {
            switch (worldState)
            {
                case GameState.Play:
                    CurrentScene.World.PauseMode = PauseModeEnum.Process;
                    break;
                case GameState.Pause:
                    CurrentScene.World.PauseMode = PauseModeEnum.Stop;
                    break;
            }

            _worldState = worldState;
            switch (uiState)
            {
                case GameState.Play:
                    CurrentScene.Gui.PauseMode = PauseModeEnum.Process;
                    Root.GuiDisableInput = false;
                    break;
                case GameState.Pause:
                    CurrentScene.Gui.PauseMode = PauseModeEnum.Stop;
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

        private void SetCurrentScene(SceneManager scene)
        {
            CurrentScene = scene;
            if (!CurrentScene.IsInsideTree()) Root.AddChild(CurrentScene);
        }
    }
}
