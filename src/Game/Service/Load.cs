using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Game.Service.Debug;
using Godot;
using Game.Abstraction;

namespace Game.Service
{
    public class Load : Node, ISingleton
    {
        public static Load Singleton { get; private set; }
        
        [Signal]
        public delegate void PropertyChanged(Object sender, string propertyName);

        private float _resourceLoadProgress;
        public float ResourceLoadProgress
        {
            get => _resourceLoadProgress;
            private set => OnPropertyChanged(ref _resourceLoadProgress, value);
        }

        public Load Init()
        {
            Singleton = this;
            return this;
        }
        
        public static T NodeAndAdd<T>(string path, Node parent, string name = "", int index = -1)
            where T : Node
        {
            PackedScene packedScene = GD.Load<PackedScene>(path);
            T node = packedScene.Instance<T>();
            AddNodeToTree(node, parent, name, index);
            return node;
        }

        public async Task<T> NodeAndAddAsync<T>(string path, Node parent, string name = "",
            int index = -1) where T : Node
        {
            PackedScene packedScene = await ResourceAsync<PackedScene>(path);
            T node = packedScene.Instance<T>();
            AddNodeToTree(node, parent, name, index);
            return node;
        }

        public static T Node<T>(string path) where T : Node
        {
            PackedScene packedScene = GD.Load<PackedScene>(path);
            T node = packedScene.Instance<T>();
            return node;
        }

        public async Task<T> NodeAsync<T>(string path) where T : Node
        {
            PackedScene packedScene = await ResourceAsync<PackedScene>(path);
            T node = packedScene.Instance<T>();
            return node;
        }

        public static void AddNodeToTree(Node node, Node parent, string name = "", int index = -1)
        {
            if (!name.Equals(""))
            {
                parent.AddChild(node);
                node.Name = name;
            }
            else parent.AddChild(node, true);
            
            if (index != -1) parent.MoveChild(node, index);
        }

        public static T Resource<T>(string path) where T : Resource => ResourceLoader.Load<T>(path);

        public static bool Resource<T>(string path, out T resource) where T : Resource
        {
            resource = null;
            if (!ResourceLoader.Exists(path)) return false;
            
            resource = ResourceLoader.Load<T>(path);
            return true;
        }

        public async Task<T> ResourceAsync<T>(string path) where T : Resource
        {
            using (ResourceInteractiveLoader loader = ResourceLoader.LoadInteractive(path))
            {
                Error err;
                do
                {
                    ResourceLoadProgress = loader.GetStage() / (float)loader.GetStageCount();
                    err = loader.Poll();
                    await ToSignal(GetTree(), "idle_frame");
                } while (err == Error.Ok && IsInstanceValid(this));
                if (err != Error.FileEof) Log.Error("Poll error!");
                Log.Info($"Resource Loaded -> {path}");
                return (T)loader.GetResource();
            }
        }

        public bool OnPropertyChanged<T>(ref T field, T value,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            EmitSignal(nameof(PropertyChanged), this, propertyName);
            return true;
        }
    }
}
