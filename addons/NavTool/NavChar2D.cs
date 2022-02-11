using Godot;

namespace NavTool
{
    [Tool]
    public class NavChar2D : Area2D
    {
        public NavBody2D NavBody { get; private set; }
        public CollisionShape2D Shape;

        public override void _Ready()
        {
            if (Engine.EditorHint) return;

            NavBody = GetParent<NavBody2D>();
            Shape = GetNode<CollisionShape2D>("CollisionShape2D");
            Shape.Position = new Vector2(0, -1);
        }
        
        public override string _GetConfigurationWarning()
        {
            if (!Engine.EditorHint) return "";
            return GetParent() is NavBody2D 
                ? "" 
                : "This node has no NavBody2D parent. Consider adding this to NavBody2D as a child.";
        }
    }
}
