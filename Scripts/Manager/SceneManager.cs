using Godot;

namespace Manager
{
    [Tool]
    public class SceneManager : Node
    {
        [Export(PropertyHint.Range, "0,1920,or_greater")]
        public Vector2 WorldScreenSize { get; private set; } = new Vector2(320, 180);
        [Export(PropertyHint.Range, "0,1920,or_greater")]
        public Vector2 ScreenSize { get; private set; } = new Vector2(1600, 900);
        
        public ViewportContainer ViewportContainer { get; private set; }
        public Viewport Viewport { get; private set; }
        public CanvasLayer World { get; private set; }
        public CanvasLayer Gui { get; private set; }

        private float Scale => ScreenSize.x / WorldScreenSize.x;

        public override void _EnterTree()
        {
            ViewportContainer = GetNode<ViewportContainer>("ViewportContainer");
            Viewport = ViewportContainer.GetNode<Viewport>("Viewport");
            World = GetNode<CanvasLayer>("World");
            Gui = GetNode<CanvasLayer>("Gui");

            if (Engine.EditorHint)
            {
                Gui.Scale = Vector2.One / Scale;
                ViewportContainer.RectScale = Vector2.One;
                ViewportContainer.RectPosition = Vector2.Zero;
                ViewportContainer.RectSize = WorldScreenSize + new Vector2(2, 2);
                Viewport.Size = ViewportContainer.RectSize;
                return;
            }
            
            GetTree().Root.Size = ScreenSize;
            Gui.Scale = Vector2.One;
            ViewportContainer.RectScale = new Vector2(Scale, Scale);
            ViewportContainer.RectPosition = new Vector2(-Scale, -Scale);
            ViewportContainer.RectSize = WorldScreenSize + new Vector2(2, 2);
            Viewport.Size = ViewportContainer.RectSize;
            RemoveChild(World);
            Viewport.AddChild(World, true);

            PauseMode = PauseModeEnum.Process;
            ViewportContainer.PauseMode = PauseModeEnum.Stop;
            Gui.PauseMode = PauseModeEnum.Stop;
        }
    }
}
