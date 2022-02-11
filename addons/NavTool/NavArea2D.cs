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
        public delegate void TargetEntered(NavChar2D navChar);
        [Signal]
        public delegate void TargetExited(NavChar2D navChar);
        [Signal]
        public delegate void ScreenEntered();
        [Signal]
        public delegate void ScreenExited();
        
        [Export]
        private NodePath _navCharPath = default;
        [Export]
        private NodePath _targetNavCharPath = default;
        
        public Vector2 AreaExtents { get; private set; }
        public Rect2 AreaRect => new Rect2(GlobalPosition - AreaExtents, AreaExtents * 2f);
        public Rect2 ReachableAreaRect => new Rect2(
            AreaRect.Position.x + NavChar.ShapeExtents.x,
            AreaRect.Position.y,
            AreaRect.Size.x - NavChar.ShapeExtents.x * 2f,
            AreaRect.Size.y
        );
        public bool IsTargetReachable { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            
            NavChar = GetNode<NavChar2D>(_navCharPath);
            TargetNavChar = GetNode<NavChar2D>(_targetNavCharPath);
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            VisibilityNotifier = GetNode<VisibilityNotifier2D>("VisibilityNotifier2D");
            
            if (_shape.Shape is RectangleShape2D shape) AreaExtents = shape.Extents;
            SetTransformsAccordingToShape();
            
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
            return !(position.x > AreaRect.End.x) &&
                   !(position.x < AreaRect.Position.x) &&
                   !(position.y > AreaRect.End.y) &&
                   !(position.y < AreaRect.Position.y);
        }
        
        public bool IsPositionReachable(Vector2 position)
        {
            return !(position.x > ReachableAreaRect.End.x) &&
                   !(position.x < ReachableAreaRect.Position.x) &&
                   !(position.y > ReachableAreaRect.End.y) &&
                   !(position.y < ReachableAreaRect.Position.y);
        }

        private void SetTransformsAccordingToShape()
        {
            GlobalPosition = _shape.GlobalPosition;
            _shape.GlobalPosition = GlobalPosition;
            VisibilityNotifier.Scale = Vector2.One;
            VisibilityNotifier.Position = Vector2.Zero;
            VisibilityNotifier.Rect = new Rect2(AreaRect.Position - GlobalPosition, AreaRect.Size);
        }

        private void OnTargetEntered(NavChar2D navChar)
        {
            if (navChar != TargetNavChar) return;
            
            IsTargetReachable = true;
            EmitSignal(nameof(TargetEntered), TargetNavChar);
        }

        private void OnTargetExited(NavChar2D navChar)
        {
            if (navChar != TargetNavChar) return;
            
            IsTargetReachable = false;
            EmitSignal(nameof(TargetExited), TargetNavChar);
        }
        
        private void OnScreenEnter()
        {
            NavChar.IsInactive = false;
            EmitSignal(nameof(ScreenEntered));
        }

        private void OnScreenExit()
        {
            NavChar.IsInactive = true;
            EmitSignal(nameof(ScreenExited));
        }

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

