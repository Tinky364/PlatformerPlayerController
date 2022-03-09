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
        
        public ViewportContainer World { get; private set; }
        public Viewport WorldViewport { get; private set; }
        public CanvasLayer Gui { get; private set; }

        private float Scale => ScreenSize.x / WorldScreenSize.x;

        public override void _EnterTree()
        {
            World = GetNode<ViewportContainer>("World");
            WorldViewport = World.GetNode<Viewport>("Viewport");
            Gui = GetNode<CanvasLayer>("Gui");

            if (Engine.EditorHint)
            {
                GD.Print("a");
                Gui.Scale = Vector2.One / Scale;
                World.RectScale = Vector2.One;
                World.RectPosition = Vector2.Zero;
                World.RectSize = WorldScreenSize + new Vector2(2, 2);
                WorldViewport.Size = World.RectSize;
                return;
            }
            
            GetTree().Root.Size = ScreenSize;
            Gui.Scale = Vector2.One;
            World.RectScale = new Vector2(Scale, Scale);
            World.RectPosition = new Vector2(-Scale, -Scale);
            World.RectSize = WorldScreenSize + new Vector2(2, 2);
            WorldViewport.Size = World.RectSize;

            PauseMode = PauseModeEnum.Process;
            World.PauseMode = PauseModeEnum.Stop;
            Gui.PauseMode = PauseModeEnum.Stop;
        }
    }
}
