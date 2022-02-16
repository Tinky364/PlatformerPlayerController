using Godot;

namespace Manager
{
    public class SceneManager : Node
    {
        public CanvasLayer World { get; private set; }
        public CanvasLayer Gui { get; private set; }

        public override void _EnterTree()
        {
            World = GetNode<CanvasLayer>("World");
            Gui = GetNode<CanvasLayer>("Gui");

            PauseMode = PauseModeEnum.Process;
            World.PauseMode = PauseModeEnum.Stop;
            Gui.PauseMode = PauseModeEnum.Stop;
        }
    }
}
