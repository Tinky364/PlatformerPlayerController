using Game.Service;
using Godot;
using Game.Abstraction;

namespace Game.Interface
{
    public class LoadingPanel : Panel
    {
        private ProgressBar _progressBar;

        public new LoadingPanel Init()
        {
            base.Init();
            _progressBar = GetNode<ProgressBar>("ProgressBar");
            App.Singleton.Connect(nameof(App.SceneLoadStarted), this, nameof(OnSceneLoadStarted));
            App.Singleton.Connect(nameof(App.SceneLoadEnded), this, nameof(OnSceneLoadEnded));
            Load.Singleton.Connect(nameof(Load.PropertyChanged), this, nameof(OnPropertyChanged));
            return this;
        }

        private void OnSceneLoadStarted(string sceneName)
        {
            _progressBar.Value = 0f;
            Visible = true;
        }

        private void OnSceneLoadEnded(string sceneName)
        {
            _progressBar.Value = 1f;
            Visible = false;
        }
        
        private void OnPropertyChanged(Object sender, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Load.ResourceLoadProgress):
                    if (sender is Load load) _progressBar.Value = load.ResourceLoadProgress;
                    break;
            }
        }
    }
}
