using Godot;

namespace PlatformerPlayerController.Scripts.Navigation
{
    public class NavArea : Area2D
    {
        public NavBody NavBody { get; private set; }
        public NavBody TargetNavBody { get; private set; }
        private CollisionShape2D _shape;
        
        [Signal]
        public delegate void TargetEntered(NavBody navBody);
        [Signal]
        public delegate void TargetExited(NavBody navBody);
        
        [Export]
        private NodePath _targetNavBodyPath = default;
        
        public Vector2 ShapeExtents { get; private set; }
        public bool IsTargetReachable { get; private set; }
    
        public override void _Ready()
        {
            _shape = GetNode<CollisionShape2D>("Shape");
            NavBody = GetNode<NavBody>("../Body/NavBody");
            TargetNavBody = GetNode<NavBody>(_targetNavBodyPath);

            if (_shape.Shape is RectangleShape2D shape) ShapeExtents = shape.Extents;
            
            Connect("area_entered", this, nameof(OnTargetEntered));
            Connect("area_exited", this, nameof(OnTargetExited));
        }

        public Vector2 DirectionToTarget() => (TargetNavBody.NavPosition - NavBody.NavPosition).Normalized();

        public float DistanceToTarget() => (TargetNavBody.NavPosition - NavBody.NavPosition).Length();

        public bool IsPositionInArea(Vector2 position)
        {
            Vector2 shapePos = _shape.GlobalPosition;
            if (position.x > shapePos.x + ShapeExtents.x || position.x < shapePos.x - ShapeExtents.x)
                return false;
            if (position.y > shapePos.y + ShapeExtents.y || position.y < shapePos.y - ShapeExtents.y)
                return false;
            return true;
        }

        private void OnTargetEntered(NavBody navBody)
        {
            if (navBody == TargetNavBody)
            {
                IsTargetReachable = true;
                EmitSignal(nameof(TargetEntered), TargetNavBody);
            }
        }

        private void OnTargetExited(NavBody navBody)
        {
            if (navBody == TargetNavBody)
            {
                IsTargetReachable = false;
                EmitSignal(nameof(TargetExited), TargetNavBody);
            }
        }
    }
}
