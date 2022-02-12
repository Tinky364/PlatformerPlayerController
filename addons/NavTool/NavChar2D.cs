using Godot;

namespace NavTool
{
    [Tool]
    public class NavChar2D : Area2D
    {
        private NavBody2D _navBody;
        public CollisionShape2D Shape { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            _navBody = GetParent<NavBody2D>();
            Shape = GetNode<CollisionShape2D>("CollisionShape2D");
            Shape.Position = new Vector2(0, -1);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;
            GlobalPosition = _navBody.NavPos;
        }

        public override string _GetConfigurationWarning()
        {
            if (!Engine.EditorHint) return "";
            return GetParent() is KinematicBody2D 
                ? "" 
                : "This node has no NavBody2D parent. Consider adding this to NavBody2D as a child.";
        }
    }
}
