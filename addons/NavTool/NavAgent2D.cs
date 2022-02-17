using Godot;

namespace NavTool
{
    public class NavAgent2D : NavBody2D
    {
        public NavArea2D NavArea { get; private set; }
        public NavBody2D TargetNavBody { get; set; }
        private Vector2 _snapVector = Vector2.Down * 2f;

        [Export]
        public bool DebugEnabled { get; private set; }
        [Export]
        private NodePath TargetNavBodyPath { get; set; }

        public bool SnapDisabled;
        
        public override void _Ready()
        {
            base._Ready();
            NavArea = GetNode<NavArea2D>("../NavArea2D");
            if (TargetNavBodyPath != null) TargetNavBody = GetNode<NavBody2D>(TargetNavBodyPath);
            
            NavArea.Connect("ScreenEntered", this, nameof(OnScreenEnter));
            NavArea.Connect("ScreenExited", this, nameof(OnScreenExit));
            
            if (!NavArea.IsPositionInArea(GlobalPosition))
                GlobalPosition = NavArea.GlobalPosition;
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            if (SnapDisabled) _snapVector = Vector2.Zero;
            else _snapVector = Vector2.Down * 2f; 
            NavArea?.CheckTargetInArea(TargetNavBody);
        }

        public Vector2 MoveInArea(Vector2 velocity, float delta, Vector2? upDirection = null)
        {
            if (NavTween.IsPlaying) velocity = NavTween.EqualizeVelocity(velocity, delta);
            
            Vector2 nextFramePos = NavPos + velocity * delta;
            switch (CurNavBodyType)
            {
                case NavBodyType.Platformer:
                    if (nextFramePos.x < NavArea.AreaRect.Position.x && velocity.x < 0 || nextFramePos.x > NavArea.AreaRect.End.x && velocity.x > 0)
                        velocity.x = 0;
                    break;
                case NavBodyType.TopDown:
                    if (nextFramePos.x < NavArea.AreaRect.Position.x && velocity.x < 0 || nextFramePos.x > NavArea.AreaRect.End.x && velocity.x > 0)
                        velocity.x = 0;
                    if (nextFramePos.y < NavArea.AreaRect.Position.y && velocity.y < 0 || nextFramePos.y > NavArea.AreaRect.End.y && velocity.y > 0)
                        velocity.y = 0;
                    break;
            }
            
            return MoveAndSlideWithSnap(velocity, _snapVector, upDirection);
        }
        
        public Vector2 DirectionToTarget()
        {
            if (TargetNavBody == null) return Vector2.Zero;
            return (TargetNavBody.NavPos - NavPos).Normalized();
        }

        public float DistanceToTarget()
        { 
            if (TargetNavBody == null) return 0;
            return (TargetNavBody.NavPos - NavPos).Length();
        }
        
        protected void OnScreenEnter() => IsInactive = false;
        
        protected void OnScreenExit() => IsInactive = true;
    }
}
