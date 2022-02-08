using Godot;

namespace NavTool
{
    [Tool]
    public class NavArea2D : Area2D
    {
        public NavChar2D NavChar { get; private set; }
        public NavChar2D TargetNavChar { get; private set; }
        public VisibilityNotifier2D VisibilityNotifier { get; private set; }
        private CollisionShape2D _shape;
        
        [Signal]
        public delegate void TargetEntered(NavChar2D navChar2D);
        [Signal]
        public delegate void TargetExited(NavChar2D navChar2D);
        
        [Export]
        private NodePath _navCharPath = default;
        [Export]
        private NodePath _targetNavCharPath = default;
        
        public Vector2 ShapeExtents { get; private set; }
        public Rect2 ShapeRect => new Rect2(GlobalPosition - ShapeExtents, ShapeExtents * 2f);
        public bool IsTargetReachable { get; private set; }
        public bool IsOnCam { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            
            NavChar = GetNode<NavChar2D>(_navCharPath);
            TargetNavChar = GetNode<NavChar2D>(_targetNavCharPath);
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            VisibilityNotifier = GetNode<VisibilityNotifier2D>("VisibilityNotifier2D");
            
            if (_shape.Shape is RectangleShape2D shape) ShapeExtents = shape.Extents;
            SetPositionToShapeCenter();
            VisibilityNotifier.Rect = new Rect2(ShapeRect.Position - GlobalPosition, ShapeRect.Size);
            
            VisibilityNotifier.Connect("screen_exited", this, nameof(OnScreenExit));
            VisibilityNotifier.Connect("screen_entered", this, nameof(OnScreenEnter));
            Connect("area_entered", this, nameof(OnTargetEntered));
            Connect("area_exited", this, nameof(OnTargetExited));
        }

        public Vector2 DirectionToTarget() =>
            (TargetNavChar.NavPosition - NavChar.NavPosition).Normalized();

        public float DistanceToTarget() => (TargetNavChar.NavPosition - NavChar.NavPosition).Length();

        public bool IsPositionInArea(Vector2 position)
        {
            if (position.x > ShapeRect.End.x || position.x < ShapeRect.Position.x)
                return false;
            if (position.y > ShapeRect.End.y || position.y < ShapeRect.Position.y)
                return false;
            return true;
        }

        private void SetPositionToShapeCenter()
        {
            GlobalPosition = _shape.GlobalPosition;
            _shape.GlobalPosition = GlobalPosition;
        }
        
        private void OnTargetEntered(NavChar2D navChar2D)
        {
            if (navChar2D == TargetNavChar)
            {
                IsTargetReachable = true;
                EmitSignal(nameof(TargetEntered), TargetNavChar);
            }
        }

        private void OnTargetExited(NavChar2D navChar2D)
        {
            if (navChar2D == TargetNavChar)
            {
                IsTargetReachable = false;
                EmitSignal(nameof(TargetExited), TargetNavChar);
            }
        }
        
        private void OnScreenEnter() => IsOnCam = true;

        private void OnScreenExit() => IsOnCam = false;
        
        public override string _GetConfigurationWarning()
        {
            if (!Engine.EditorHint) return "";
            for (int i = 0; i < GetChildCount(); i++)
            {
                if (GetChild(i) is VisibilityNotifier2D)
                    return "";
            }
            return "This node has no VisibilityNotifier2D. Consider adding a VisibilityNotifier2D" +
                       " as a child.";
        }
    }
}

