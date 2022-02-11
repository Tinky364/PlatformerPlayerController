using Godot;
using Godot.Collections;

namespace NavTool
{
    [Tool]
    public class NavChar2D : Area2D
    {
        private KinematicBody2D _body;
        private CollisionShape2D _shape;
        private NavArea2D _navArea;
        public Tween Tween { get; }
        private Physics2DDirectSpaceState _spaceState;
        
        [Export]
        private NodePath _navAreaPath = default;
        [Export(PropertyHint.Range, "0.1,200,or_greater")] 
        private float _groundRayLength = 35f;

        private Dictionary _ray;
        public Vector2 NavPosition { get; private set; } = new Vector2();
        public Vector2 ShapeExtents { get; private set; }

        private bool _isInactive;
        public bool IsInactive
        {
            get => _isInactive;
            set
            {
                _isInactive = value;
                SetProcess(!value);
                SetPhysicsProcess(!value);
            }
        }

        private NavChar2D()
        {
            Tween = new Tween();
            AddChild(Tween);
            Tween.Name = "Tween";
            Tween.PlaybackProcessMode = Tween.TweenProcessMode.Physics;
        }

        public override void _Ready()
        {
            if (Engine.EditorHint) return;

            _body = GetNode<KinematicBody2D>("..");
            _shape = GetNode<CollisionShape2D>("CollisionShape2D");
            if (_navAreaPath != default) _navArea = GetNodeOrNull<NavArea2D>(_navAreaPath);
            _spaceState = GetWorld2d().DirectSpaceState;
            if (_shape.Shape is RectangleShape2D shape) ShapeExtents = shape.Extents;

            if (_navArea != null && !_navArea.IsPositionInArea(GlobalPosition))
                SetBodyPosition(_navArea.GlobalPosition);

            FindGroundPositionForArea();
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;

            FindGroundPositionForArea();
        }

        public Vector2 DirectionTo(Vector2 to) => NavPosition.DirectionTo(to);

        public float DistanceTo(Vector2 to) => NavPosition.DistanceTo(to);

        public void LerpWithDuration(float targetPosX, float duration,
                                     Tween.TransitionType transitionType = Tween.TransitionType.Linear,
                                     Tween.EaseType easeType = Tween.EaseType.InOut,
                                     float delay = 0f)
        {
            if (targetPosX < _navArea.ReachableAreaRect.Position.x)
                targetPosX = _navArea.ReachableAreaRect.Position.x;
            else if (targetPosX > _navArea.ReachableAreaRect.End.x)
                targetPosX = _navArea.ReachableAreaRect.End.x;

            Tween.InterpolateProperty(
                _body,
                "global_position:x",
                null,
                targetPosX,
                duration,
                transitionType,
                easeType,
                delay
            );
            Tween.Start();
        }
        
        public void LerpWithSpeed(float targetPosX, float speed,
                                  Tween.TransitionType transitionType = Tween.TransitionType.Linear,
                                  Tween.EaseType easeType = Tween.EaseType.InOut,
                                  float delay = 0f)
        {
            if (targetPosX < _navArea.ReachableAreaRect.Position.x)
                targetPosX = _navArea.ReachableAreaRect.Position.x;
            else if (targetPosX > _navArea.ReachableAreaRect.End.x)
                targetPosX = _navArea.ReachableAreaRect.End.x;

            Tween.InterpolateProperty(
                _body,
                "global_position:x",
                null,
                targetPosX,
                Mathf.Abs(targetPosX - GlobalPosition.x) / speed,
                transitionType,
                easeType,
                delay
            );
            Tween.Start();
        }

        public void StopLerp()
        {
            Tween.Stop(_body, "global_position:x");
        }

        private void SetBodyPosition(Vector2 position)
        {
            _body.GlobalPosition = position;
        }

        public Vector2 MoveAndSlide(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (_body.GlobalPosition.x + velocity.x * delta < _navArea.AreaRect.Position.x &&
                velocity.x < 0
                || _body.GlobalPosition.x + velocity.x * delta > _navArea.AreaRect.End.x &&
                velocity.x > 0)
            {
                velocity.x = 0;
            }

            return _body.MoveAndSlide(velocity, upDirection);
        }

        private void FindGroundPositionForArea()
        {
            Vector2 rightHitPos;

            // Cast left ray.
            _ray = _spaceState.IntersectRay(
                _body.GlobalPosition + new Vector2(-ShapeExtents.x, 0f),
                _body.GlobalPosition + new Vector2(-ShapeExtents.x, _groundRayLength),
                new Array {this, _body},
                _body.CollisionMask
            );

            if (_ray.Count > 0) // When left ray hits.
            {
                Vector2 leftHitPos = (Vector2) _ray["position"]; // Left ray hit position.

                // Cast right ray.
                _ray = _spaceState.IntersectRay(
                    _body.GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    _body.GlobalPosition + new Vector2(ShapeExtents.x, _groundRayLength),
                    new Array {this, _body},
                    _body.CollisionMask
                );
                if (_ray.Count > 0) // When both rays hit.
                {
                    rightHitPos = (Vector2) _ray["position"]; // Right ray hit position.

                    // Decides position according to the close hit position.
                    if (rightHitPos.DistanceTo(_body.GlobalPosition) >
                        leftHitPos.DistanceTo(_body.GlobalPosition))
                        NavPosition = leftHitPos + new Vector2(ShapeExtents.x, 0);
                    else
                        NavPosition = rightHitPos + new Vector2(-ShapeExtents.x, 0);
                }
                else // When only left ray hits.
                    NavPosition = leftHitPos + new Vector2(ShapeExtents.x, 0);
            }
            else // When left ray does not hit.
            {
                // Cast right ray.
                _ray = _spaceState.IntersectRay(
                    _body.GlobalPosition + new Vector2(ShapeExtents.x, 0f),
                    _body.GlobalPosition + new Vector2(ShapeExtents.x, _groundRayLength),
                    new Array {this, _body},
                    _body.CollisionMask
                );
                if (_ray.Count > 0) // When only right ray hits. 
                {
                    rightHitPos = (Vector2) _ray["position"]; // Right ray hit position.

                    NavPosition = rightHitPos + new Vector2(-ShapeExtents.x, 0);
                }
                else // When any rays do not hit.
                {
                    NavPosition = _body.GlobalPosition;
                }
            }

            GlobalPosition = NavPosition;
        }

        public override string _GetConfigurationWarning()
        {
            if (!Engine.EditorHint) return "";

            if (GetNodeOrNull<KinematicBody2D>("..") == null)
                return "Please only use it as a child of KinematicBody2D.";

            return "";
        }
    }
}
