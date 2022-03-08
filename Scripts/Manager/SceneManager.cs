using Godot;

namespace Manager
{
    public class SceneManager : Node
    {
        public ViewportContainer World { get; private set; }
        public CanvasLayer Gui { get; private set; }

        public override void _EnterTree()
        {
            World = GetNode<ViewportContainer>("World");
            Gui = GetNode<CanvasLayer>("Gui");

            World.RectScale = new Vector2(5, 5);
            World.RectPosition = new Vector2(-5, -5);
            Gui.Scale = Vector2.One;

            PauseMode = PauseModeEnum.Process;
            World.PauseMode = PauseModeEnum.Stop;
            Gui.PauseMode = PauseModeEnum.Stop;
        }
    }
}
