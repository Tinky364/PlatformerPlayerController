using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts.Navigation
{
    public class NavBody : Area2D
    {
        private KinematicBody2D _body;
        private CollisionShape2D _collisionShape;
        private RectangleShape2D _shape;
        private Physics2DDirectSpaceState _spaceState;

        public Vector2 NavPosition { get; private set; } = new Vector2();
        
        public override void _Ready()
        {
            _body = GetNode<KinematicBody2D>("..");
            _collisionShape = GetNode<CollisionShape2D>("Shape");
            _shape = (RectangleShape2D) _collisionShape.Shape;
            _spaceState = GetWorld2d().DirectSpaceState;
            FindGroundPositionForBody();
        }

        public override void _Process(float delta)
        {
            FindGroundPositionForBody();
        }

        private void FindGroundPositionForBody()
        {
            Dictionary raycast = _spaceState.IntersectRay(
                _body.Position,
                _body.Position + Vector2.Down * 300f,
                new Array {this, _body},
                _body.CollisionMask
            );

            if (raycast.Count <= 0) return;
            NavPosition = (Vector2) raycast["position"];
            GlobalPosition = NavPosition;
        }
    }
}
