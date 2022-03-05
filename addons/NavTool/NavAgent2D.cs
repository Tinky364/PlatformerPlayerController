using Godot;

namespace NavTool
{
    public class NavAgent2D : NavBody2D
    {
        [Export]
        public bool DebugEnabled { get; private set; }
        [Export]
        private bool _isOnBodyCollidingActive;
        [Export]
        private NodePath TargetNavBodyPath { get; set; }

        [Signal]
        public delegate void ScreenEntered();
        [Signal]
        public delegate void ScreenExited();
        [Signal]
        protected delegate void BodyColliding(Node body);
        
        public NavArea2D NavArea { get; private set; }
        public NavBody2D TargetNavBody { get; set; }
        
        public bool SnapDisabled;
        protected PhysicsBody2D CollidingBody { get; private set; }
        private Vector2 SnapVector => SnapDisabled ? Vector2.Zero : Vector2.Down * 2f;
        private bool _isColliding;

        public override void _Ready()
        {
            base._Ready();
            NavArea = GetNode<NavArea2D>("../NavArea2D");
            if (TargetNavBodyPath != null) TargetNavBody = GetNode<NavBody2D>(TargetNavBodyPath);
            
            NavArea.Connect("ScreenEntered", this, nameof(OnScreenEnter));
            NavArea.Connect("ScreenExited", this, nameof(OnScreenExit));

            if (!NavArea.IsPositionInArea(GlobalPosition)) GlobalPosition = NavArea.GlobalPosition;
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            OnBodyColliding(CollidingBody);
            NavArea?.CheckTargetInArea(TargetNavBody);
        }

        public Vector2 MoveInArea(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (NavTween.IsPlaying) velocity = NavTween.EqualizeVelocity(velocity, delta);
            
            Vector2 nextFramePos = NavPos + velocity * delta;
            switch (CurNavBodyType)
            {
                case NavBodyType.Platformer:
                    if (nextFramePos.x < NavArea.AreaRect.Position.x && velocity.x < 0 ||
                        nextFramePos.x > NavArea.AreaRect.End.x && velocity.x > 0)
                        velocity.x = 0;
                    break;
                case NavBodyType.TopDown:
                    if (nextFramePos.x < NavArea.AreaRect.Position.x && velocity.x < 0 ||
                        nextFramePos.x > NavArea.AreaRect.End.x && velocity.x > 0)
                        velocity.x = 0;
                    if (nextFramePos.y < NavArea.AreaRect.Position.y && velocity.y < 0 ||
                        nextFramePos.y > NavArea.AreaRect.End.y && velocity.y > 0)
                        velocity.y = 0;
                    break;
            }
            
            return MoveAndSlideWithSnap(velocity, SnapVector, upDirection);
        }

        public Vector2 DirectionToTarget() =>
            TargetNavBody == null ? Vector2.Zero : (TargetNavBody.NavPos - NavPos).Normalized();

        public float DistanceToTarget() =>
            TargetNavBody == null ? 0 : (TargetNavBody.NavPos - NavPos).Length();

        protected void OnScreenEnter() => EmitSignal(nameof(ScreenEntered));
        
        protected void OnScreenExit() => EmitSignal(nameof(ScreenExited));
        
        protected override void OnBodyEntered(Node node)
        {
            if (!(node is PhysicsBody2D body)) return;
            _isColliding = true;
            CollidingBody = body;
            EmitSignal(nameof(BodyEntered), node);
        }

        protected override void OnBodyExited(Node node)
        {
            if (!(node is PhysicsBody2D body)) return;
            if (body != CollidingBody) return;
            _isColliding = false;
            CollidingBody = null;
            EmitSignal(nameof(BodyExited), node);
        }
        
        protected virtual void OnBodyColliding(Node body)
        {
            if (!_isOnBodyCollidingActive) return;
            if (!_isColliding) return;
            EmitSignal(nameof(BodyColliding), body);
        }
    }
}
