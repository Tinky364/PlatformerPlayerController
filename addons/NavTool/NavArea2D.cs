using Godot;

namespace NavTool
{
    [Tool]
    public class NavArea2D : Area2D
    {
        private VisibilityNotifier2D _visibilityNotifier;
        private CollisionShape2D _shape;
        
        [Signal]
        public delegate void TargetEntered(NavBody2D navBody);
        [Signal]
        public delegate void TargetExited(NavBody2D navBody);
        [Signal]
        public delegate void ScreenEntered();
        [Signal]
        public delegate void ScreenExited();
       
        public Vector2 AreaExtents { get; private set; }
        public Rect2 AreaRect => new Rect2(GlobalPosition - AreaExtents, AreaExtents * 2f);
        public bool IsTargetReachable { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            _visibilityNotifier = GetNode<VisibilityNotifier2D>("VisibilityNotifier2D");
            
            if (_shape.Shape is RectangleShape2D shape) AreaExtents = shape.Extents;
            SetTransformsAccordingToShape();
            
            _visibilityNotifier.Connect("screen_exited", this, nameof(OnScreenExit));
            _visibilityNotifier.Connect("screen_entered", this, nameof(OnScreenEnter));
        }

        public bool IsPositionInArea(Vector2 position)
        {
            return !(position.x > AreaRect.End.x) &&
                   !(position.x < AreaRect.Position.x) &&
                   !(position.y > AreaRect.End.y) &&
                   !(position.y < AreaRect.Position.y);
        }
        
        private void SetTransformsAccordingToShape()
        {
            GlobalPosition = _shape.GlobalPosition;
            _shape.GlobalPosition = GlobalPosition;
            _visibilityNotifier.Scale = Vector2.One;
            _visibilityNotifier.Position = Vector2.Zero;
            _visibilityNotifier.Rect = new Rect2(AreaRect.Position - GlobalPosition, AreaRect.Size);
        }

        public void CheckTargetInArea(NavBody2D target)
        {
            if (target == null) return;
            Vector2 point1 = target.NavPos + new Vector2(target.ShapeExtents.x, 0);
            Vector2 point2 = target.NavPos - new Vector2(target.ShapeExtents.x, 0);
            if (IsPositionInArea(point1) || IsPositionInArea(point2))
            {
                IsTargetReachable = true;
                EmitSignal(nameof(TargetEntered), target);
            }
            else
            {
                if (IsTargetReachable)
                {
                    IsTargetReachable = false;
                    EmitSignal(nameof(TargetExited), target);
                }
            }
        }
        
        private void OnScreenEnter()
        {
            SetProcess(true);
            SetPhysicsProcess(true);
            EmitSignal(nameof(ScreenEntered));
            Visible = true;
        }

        private void OnScreenExit()
        {
            SetProcess(false);
            SetPhysicsProcess(false);
            EmitSignal(nameof(ScreenExited));
            Visible = false;
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

