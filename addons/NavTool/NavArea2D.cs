using Godot;

namespace NavTool
{
    [Tool]
    public class NavArea2D : Area2D
    {
        public NavBody2D NavBody { get; private set; }
        public VisibilityNotifier2D VisibilityNotifier { get; private set; }
        private CollisionShape2D _shape;
        
        [Signal]
        public delegate void TargetEntered(NavBody2D navBody);
        [Signal]
        public delegate void TargetExited(NavBody2D navBody);
        [Signal]
        public delegate void ScreenEntered();
        [Signal]
        public delegate void ScreenExited();
        
        [Export]
        private NodePath _navBodyPath = default;
       
        public Vector2 AreaExtents { get; private set; }
        public Rect2 AreaRect => new Rect2(GlobalPosition - AreaExtents, AreaExtents * 2f);
        public Rect2 ReachableAreaRect => new Rect2(
            AreaRect.Position.x + NavBody.ShapeExtents.x,
            AreaRect.Position.y,
            AreaRect.Size.x - NavBody.ShapeExtents.x * 2f,
            AreaRect.Size.y
        );
        public bool IsTargetReachable { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            
            NavBody = GetNode<NavBody2D>(_navBodyPath);
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
            (NavBody.TargetNavBody.NavPos - NavBody.NavPos).Normalized();

        public float DistanceToTarget() => (NavBody.TargetNavBody.NavPos - NavBody.NavPos).Length();

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

        private void OnTargetEntered(Area2D area2D)
        {
            if ((NavChar2D) area2D != NavBody.TargetNavBody.NavChar) return;
            
            IsTargetReachable = true;
            EmitSignal(nameof(TargetEntered), NavBody.TargetNavBody);
        }

        private void OnTargetExited(Node area2D)
        {
            if ((NavChar2D) area2D != NavBody.TargetNavBody.NavChar) return;
            
            IsTargetReachable = false;
            EmitSignal(nameof(TargetExited), NavBody.TargetNavBody);
        }
        
        private void OnScreenEnter()
        {
            NavBody.IsInactive = false;
            EmitSignal(nameof(ScreenEntered));
        }

        private void OnScreenExit()
        {
            NavBody.IsInactive = true;
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

