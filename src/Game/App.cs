using Game.Interface;
using Game.Scene;
using Game.Service;
using Game.Service.Debug;
using Game.Abstraction;
using Godot;

namespace Game
{
    public class App : Node, ISingleton
    {
	    public static App Singleton { get; private set; }
	    
        [Signal]
		public delegate void SceneLoadStarted(string sceneName);
		[Signal]
		public delegate void SceneLoadEnded(string sceneName);
		
		[Export]
		private string _startingSceneName = "MenuScene";
		[Export]
		private bool _editorMode = false;
		
		public SceneTree Tree => GetTree();
		public Viewport Root => Tree.Root;
		
		public enum GameState { Play, Pause }
		public GameState LevelState { get; private set; }
		public GameState InterfaceState { get; private set; }
		
		public ViewportContainer ViewportContainer { get; private set; }
		public Viewport Viewport { get; private set; }
		public Control InterfaceContainer { get; private set; }
		public Load Load { get; private set; }
		public TreeTimer TreeTimer { get; private set; }
		public DebugOverlay DebugOverlay { get; private set; }
		public InputInvoker InputInvoker { get; private set; }
		public Events Events { get; private set; }
		
		private SceneBase _currentScene;
		public SceneBase CurrentScene
		{
			get => _currentScene;
			private set
			{
				_currentScene = value;
				Log.Info($"Current Scene -> {_currentScene.SceneName}");
			}
		}

		public override void _EnterTree()
		{
			base._EnterTree();
			Log.Info($"{Name} EnterTree");
			Singleton = this;
		}

		public override void _ExitTree()
		{
			base._ExitTree();
			Log.Info($"{Name} ExitTree");
		}

		public override void _Ready()
		{
			Log.Info($"{Name} Ready");

			ViewportContainer = GetNode<ViewportContainer>("ViewportContainer");
			Viewport = ViewportContainer.GetNode<Viewport>("Viewport");
			InterfaceContainer = GetNode<Control>("InterfaceContainer");
			Events = GetNode<Events>("Events").Init();
			InputInvoker = GetNode<InputInvoker>("InputInvoker").Init();
			Load = GetNode<Load>("Load").Init();
			TreeTimer = GetNode<TreeTimer>("TreeTimer").Init();
			DebugOverlay = GetNode<DebugOverlay>("DebugOverlay").Init();
			InterfaceContainer.GetNode<LoadingPanel>("LoadingPanel").Init();
			AppStartup();
		}

		private void AppStartup()
		{
			Log.Info("App Startup");

			if (_editorMode)
			{
				foreach (Node child in Viewport.GetChildren())
				{
					if (!(child is SceneBase scene)) continue;
					string sceneName = scene.SceneName;
					CurrentScene = scene;
					LoadSceneAsync(SceneBase.GetPath(sceneName));
					return;
				}
				LoadSceneAsync(SceneBase.GetPath(_startingSceneName));
				return;
			}
			
			LoadSceneAsync(SceneBase.GetPath(_startingSceneName)); 
		}
		
		public async void LoadSceneAsync(string path)
		{
			SetGameState(GameState.Pause, GameState.Pause);
			await ToSignal(Tree, "idle_frame");
			EmitSignal(nameof(SceneLoadStarted), CurrentScene?.SceneName);
			FreeScene(CurrentScene);
			while (IsInstanceValid(CurrentScene)) await ToSignal(GetTree(), "idle_frame");
			CurrentScene = await Load.NodeAsync<SceneBase>(path);
			AddSceneToTree(CurrentScene);
			CurrentScene.Init(this, Load, TreeTimer, DebugOverlay);
			EmitSignal(nameof(SceneLoadEnded), CurrentScene.SceneName);
			SetGameState(GameState.Play, GameState.Play);
			CurrentScene.Start();
		}

		private void AddSceneToTree(SceneBase sceneBase)
		{
			sceneBase.GetLevelAndInterface();
			sceneBase.RemoveLevelAndInterface();
			Load.AddNodeToTree(sceneBase, this, "", 0);
			Load.AddNodeToTree(sceneBase.Level, Viewport, "Level", 0);
			Load.AddNodeToTree(sceneBase.Interface, InterfaceContainer, "Interface", 0);
		}

		private void FreeScene(SceneBase scene)
		{
			if (!IsInstanceValid(scene)) return;
			scene.Level?.QueueFree();
			scene.Interface?.QueueFree();
			scene.QueueFree();
		}
		
		public void SetGameState(GameState levelState, GameState interfaceState)
		{
			if (CurrentScene != null)
			{
				switch (levelState)
				{
					case GameState.Play:
						CurrentScene.Level.PauseMode = PauseModeEnum.Process;
						break;
					case GameState.Pause:
						CurrentScene.Level.PauseMode = PauseModeEnum.Stop;
						break;
				}
				LevelState = levelState;
            
				switch (interfaceState)
				{
					case GameState.Play:
						CurrentScene.Interface.PauseMode = PauseModeEnum.Process;
						Root.GuiDisableInput = false;
						break;
					case GameState.Pause:
						CurrentScene.Interface.PauseMode = PauseModeEnum.Stop;
						Root.GuiDisableInput = true;
						break;
				}
				InterfaceState = interfaceState;
			}

			if (levelState == GameState.Pause || interfaceState == GameState.Pause)
			{
				Tree.Paused = true;
				Physics2DServer.SetActive(true);
			}
			else Tree.Paused = false;
		}
        
		public void QuitGame() => Tree.Quit();
		
		public static void SetNodeActive(Node node, bool value)
		{
			if (!IsInstanceValid(node)) return;
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
    }
}
