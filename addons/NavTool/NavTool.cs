#if TOOLS
using Godot;

namespace NavTool
{
    [Tool]
    public class NavTool : EditorPlugin
    {
        public override void _EnterTree()
        {
            var scriptArea = GD.Load<Script>("addons/NavTool/NavArea2D.cs");
            var scriptChar = GD.Load<Script>("addons/NavTool/NavChar2D.cs");

            AddCustomType("NavArea2D", "Area2D", scriptArea, null);
            AddCustomType("NavChar2D", "Area2D", scriptChar, null);
        }

        public override void _ExitTree()
        {
            RemoveCustomType("NavArea2D");
            RemoveCustomType("NavChar2D");
        }
    }
}
#endif

