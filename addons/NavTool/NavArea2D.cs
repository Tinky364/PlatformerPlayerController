using Godot;

namespace NavTool
{
    [Tool]
    public class NavArea2D : Area2D
    {
        private NavBody2D _navBody;
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
        [Export]
        private NodePath _navBodyPath = default;
       
        public Vector2 AreaExtents { get; private set; }
        public Rect2 AreaRect => new Rect2(GlobalPosition - AreaExtents, AreaExtents * 2f);
        public Rect2 ReachableAreaRect => new Rect2(
            AreaRect.Position.x + _navBody.ShapeExtents.x,
            AreaRect.Position.y,
            AreaRect.Size.x - _navBody.ShapeExtents.x * 2f,
            AreaRect.Size.y
        );
        public bool IsTargetReachable { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            
            _navBody = GetNode<NavBody2D>(_navBodyPath);
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            _visibilityNotifier = GetNode<VisibilityNotifier2D>("VisibilityNotifier2D");
            
            if (_shape.Shape is RectangleShape2D shape) AreaExtents = shape.Extents;
            SetTransformsAccordingToShape();
            
            _visibilityNotifier.Connect("screen_exited", this, nameof(OnScreenExit));
            _visibilityNotifier.Connect("screen_entered", this, nameof(OnScreenEnter));
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;
            CheckTarget();
        }

        public Vector2 DirectionToTarget() =>
            (_navBody.TargetNavBody.NavPos - _navBody.NavPos).Normalized();

        public float DistanceToTarget() => (_navBody.TargetNavBody.NavPos - _navBody.NavPos).Length();

        public bool IsPositionInArea(Vector2 position)
        {
            return !(position.x > AreaRect.End.x) &&
                   !(position.x < AreaRect.Position.x) &&
                   !(position.y > AreaRect.End.y) &&
                   !(position.y < AreaRect.Position.y);
        }
        
        public bool IsPositionInReachableArea(Vector2 position)
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
            _visibilityNotifier.Scale = Vector2.One;
            _visibilityNotifier.Position = Vector2.Zero;
            _visibilityNotifier.Rect = new Rect2(AreaRect.Position - GlobalPosition, AreaRect.Size);
        }

        private void CheckTarget()
        {
            Vector2 point1 = _navBody.TargetNavBody.NavPos + new Vector2(_navBody.TargetNavBody.ShapeExtents.x, 0);
            Vector2 point2 = _navBody.TargetNavBody.NavPos - new Vector2(_navBody.TargetNavBody.ShapeExtents.x, 0);
            if (IsPositionInArea(point1) || IsPositionInArea(point2))
            {
                IsTargetReachable = true;
                EmitSignal(nameof(TargetEntered), _navBody.TargetNavBody);
            }
            else
            {
                if (IsTargetReachable)
                {
                    IsTargetReachable = false;
                    EmitSignal(nameof(TargetExited), _navBody.TargetNavBody);
                }
            }
        }
        
        private void OnScreenEnter()
        {
            SetProcess(true);
            SetPhysicsProcess(true);
            EmitSignal(nameof(ScreenEntered));
        }

        private void OnScreenExit()
        {
            SetProcess(false);
            SetPhysicsProcess(false);
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

