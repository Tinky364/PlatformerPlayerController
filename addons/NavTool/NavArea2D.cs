using Godot;

namespace NavTool
{
    [Tool]
    public class NavArea2D : Area2D
    {
        [Signal]
        public delegate void TargetEntered(NavBody2D navBody);
        [Signal]
        public delegate void TargetExited(NavBody2D navBody);
        [Signal]
        public delegate void ScreenEntered();
        [Signal]
        public delegate void ScreenExited();
        
        public Rect2 AreaRect { get; private set; }
        public bool IsTargetReachable { get; private set; }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            SetAreaRect();
            SetVisibilityNotification();
        }

        public bool IsPositionInArea(Vector2 position)
        {
            return !(position.x > AreaRect.End.x) && !(position.x < AreaRect.Position.x) &&
                   !(position.y > AreaRect.End.y) && !(position.y < AreaRect.Position.y);
        }
        
        public void CheckTargetInArea(NavBody2D target)
        {
            if (target == null) return;
            Vector2 point1 = target.NavPos + new Vector2(target.ExtentsHalf.x, 0);
            Vector2 point2 = target.NavPos - new Vector2(target.ExtentsHalf.x, 0);
            if (IsPositionInArea(point1) || IsPositionInArea(point2))
            {
                IsTargetReachable = true;
                EmitSignal(nameof(TargetEntered), target);
            }
            else if (IsTargetReachable)
            {
                IsTargetReachable = false;
                EmitSignal(nameof(TargetExited), target);
            }
        }
        
        private void SetAreaRect()
        {
            if (!(GetNode<CollisionShape2D>("CollisionShape2D").Shape is SegmentShape2D shape))
                return;
            Vector2 areaExtents = new Vector2((shape.B.x - shape.A.x) / 2f, 4f);
            AreaRect = new Rect2(ToGlobal(shape.A + new Vector2(0, -4f)), areaExtents * 2f);
        }

        private void SetVisibilityNotification()
        {
            VisibilityNotifier2D notifier = GetNode<VisibilityNotifier2D>("VisibilityNotifier2D");
            notifier.Scale = Vector2.One;
            notifier.Position = Vector2.Zero;
            notifier.Rect = new Rect2(ToLocal(AreaRect.Position), AreaRect.Size);
            notifier.Connect("screen_exited", this, nameof(OnScreenExit));
            notifier.Connect("screen_entered", this, nameof(OnScreenEnter));
        }

        private void OnScreenEnter() => EmitSignal(nameof(ScreenEntered));

        private void OnScreenExit() => EmitSignal(nameof(ScreenExited));

        public override string _GetConfigurationWarning()
        {
            if (!Engine.EditorHint) return "";
            
            for (int i = 0; i < GetChildCount(); i++)
            {
                if (GetChild(i) is VisibilityNotifier2D) return "";
            }

            return "This node has no VisibilityNotifier2D. Consider adding a VisibilityNotifier2D" +
                   " as a child.";
        }
    }
}

