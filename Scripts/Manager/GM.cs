using System.Threading.Tasks;
using Godot;

namespace Manager
{
    public class GM : Singleton<GM>
    {
        public SceneManager CurrentScene { get; private set; }
        private SceneTree Tree => GetTree();
        private Viewport Root => Tree.Root;

        public enum GameState { Play, Pause }
        public GameState WorldState => _worldState;
        public GameState UiState => _uiState;
        private GameState _worldState;
        private GameState _uiState;

        public override void _EnterTree() { SetSingleton(); }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            Events.S.PauseMode = PauseModeEnum.Process;
            TreeTimer.S.PauseMode = PauseModeEnum.Process;
            InputManager.S.PauseMode = PauseModeEnum.Process;
            
            if (Root.GetChild(Root.GetChildCount() - 1) is SceneManager scene)
                SetCurrentScene(scene);
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
            else Tree.Paused = false;
        }

        
        public void QuitGame() => Tree.Quit();

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
        
        public static void SetNodeActive(Node node, bool value)
        {
            node.SetProcess(value);
            node.SetPhysicsProcess(value);
            node.SetProcessInput(value);
            switch (node)
            {
                case CollisionShape2D collisionShape2D:
                    //collisionShape2D.SetDeferred("disabled", !value);
                    collisionShape2D.Visible = value;
                    break;
                case CanvasItem canvasItem: canvasItem.Visible = value;
                    break;
            }
            foreach (Node child in node.GetChildren()) SetNodeActive(child, value);
        }

        public static void Print(bool debug, params object[] what)
        {
            if (!debug) return;
            GD.Print(what);
        }
        
        private async Task UnloadCurrentScene()
        {
            CurrentScene?.QueueFree();
            while (IsInstanceValid(CurrentScene)) await ToSignal(Tree, "idle_frame");
        }

        private void SetCurrentScene(SceneManager scene)
        {
            CurrentScene = scene;
            if (!CurrentScene.IsInsideTree()) Root.AddChild(CurrentScene);
        }
    }
}
