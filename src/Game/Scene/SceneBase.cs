using Game.Service;
using Game.Service.Debug;
using Godot;
using Game.Abstraction;

namespace Game.Scene
{
    public class SceneBase : Node
    {
        public static string GetPath(string name) => $"res://scenes/{name}.tscn";
        public string SceneName => Name;
        public string Path => $"res://scenes/{SceneName}.tscn";
        public Node2D Level { get; private set; }
        public Control Interface { get; private set; }

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                Level.Visible = value;
                Interface.Visible = value;
            }
        }

        public virtual SceneBase Init(App app, Load load, TreeTimer treeTimer, DebugOverlay debugOverlay)
        {
            Log.Info($"{Name} Init");
            Level.Scale = Vector2.One;
            Visible = false;
            return this;
        }
        
        public virtual void Start()
        {
            Log.Info($"{Name} Start");
            Visible = true;
        }
        
        public override void _ExitTree()
        {
            base._ExitTree();
            Log.Info($"{Name} ExitTree");
        }
        
        public void GetLevelAndInterface()
        {
            Level = GetNode<Node2D>("Level");
            Interface = GetNode<Control>("Interface");
        }

        public void RemoveLevelAndInterface()
        {
            RemoveChild(Level);
            RemoveChild(Interface);
        }
    }
}
