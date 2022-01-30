using Godot;

namespace PlatformerPlayerController.Scripts.Navigation
{
    public class NavArea : Area2D
    {
        [Signal]
        public delegate void TargetEntered(NavBody navBody);
        [Signal]
        public delegate void TargetExited(NavBody navBody);
        
        [Export]
        private NodePath _navBodyPath = default;
        [Export]
        private NodePath _targetNavBodyPath = default;
        
        private CollisionShape2D _collisionShape;
        private RectangleShape2D _shape;
        public NavBody TargetNavBody { get; private set; }
        public NavBody NavBody { get; private set; }
        
        public bool IsTargetReachable { get; private set; }
    
        public override void _Ready()
        {
            _collisionShape = GetNode<CollisionShape2D>("Shape");
            _shape = _collisionShape.Shape as RectangleShape2D;
            NavBody = GetNode<NavBody>(_navBodyPath);
            TargetNavBody = GetNode<NavBody>(_targetNavBodyPath);
            Connect("area_entered", this, nameof(OnTargetEntered));
            Connect("area_exited", this, nameof(OnTargetExited));
        }

        public Vector2 DirectionToTarget() => (TargetNavBody.NavPosition - NavBody.NavPosition).Normalized();

        public float DistanceToTarget() => (TargetNavBody.NavPosition - NavBody.NavPosition).Length();

        public bool IsPositionInArea(Vector2 position)
        {
            Vector2 shapePos = _collisionShape.GlobalPosition;
            if (position.x > shapePos.x + _shape.Extents.x || position.x < shapePos.x - _shape.Extents.x)
                return false;
            if (position.y > shapePos.y + _shape.Extents.y || position.y < shapePos.y - _shape.Extents.y)
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
