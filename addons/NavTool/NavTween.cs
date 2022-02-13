using Godot;

namespace NavTool
{
    public class NavTween : Tween
    {
        private Node2D _connectedNode;

        private Vector2 _lerpPos;
        public Vector2 LerpPos => _lerpPos;
        private Vector2 _velocity;
        public Vector2 Velocity => _velocity;

        public enum LerpingMode { Vector2, X, Y }
        public LerpingMode CurLerpingMode { get; private set; }
        
        public bool IsLerping { get; private set; }

        public override void _Ready()
        {
            PlaybackProcessMode = TweenProcessMode.Physics;
            _connectedNode = GetParent<Node2D>();
            Connect("tween_started", this, nameof(OnMoveLerpStarted));
            Connect("tween_completed", this, nameof(OnMoveLerpCompleted));
        }

        public override void _PhysicsProcess(float delta)
        {
            CalculateLerpVelocity(delta);
        }

        public void MoveLerp(LerpingMode mode, Vector2 targetPos, float duration, TransitionType transitionType = TransitionType.Linear, EaseType easeType = EaseType.InOut, float delay = 0f)
        {
            if (IsLerping)
            {
                GD.Print("Already lerping wait until finish or call StopLerp method!");
                return;
            }

            CurLerpingMode = mode;
            InterpolateProperty(this, "_lerpPos", null, targetPos, duration, transitionType, easeType, delay);
            Start();
        }

        public void MoveLerpWithSpeed(
            LerpingMode mode,
            Vector2 targetPos,
            float speed,
            TransitionType transitionType = TransitionType.Linear,
            EaseType easeType = EaseType.InOut,
            float delay = 0f)
        {
            float duration = targetPos.DistanceTo(_connectedNode.GlobalPosition) / speed;
            MoveLerp(mode, targetPos, duration, transitionType, easeType, delay);
        }

        private void CalculateLerpVelocity(float delta)
        {
            if (IsLerping)
            {
                _velocity = _connectedNode.GlobalPosition.DirectionTo(_lerpPos) * _connectedNode
                    .GlobalPosition.DistanceTo(_lerpPos) / delta;
            }
            else
            {
                _lerpPos = _connectedNode.GlobalPosition;
            }
        }

        public void EqualizeVelocity(ref Vector2 value)
        {
            if (!IsLerping) return;
            switch (CurLerpingMode)
            {
                case LerpingMode.Vector2:
                    value = _velocity;
                    break;
                case LerpingMode.X:
                    value.x = _velocity.x;
                    break;
                case LerpingMode.Y:
                    value.y = _velocity.y;
                    break;
            }
        }

        public void EqualizePosition(ref Vector2 value)
        {
            if (!IsLerping) return;
            switch (CurLerpingMode)
            {
                case LerpingMode.Vector2:
                    value = _lerpPos;
                    break;
                case LerpingMode.X:
                    value.x = _lerpPos.x;
                    break;
                case LerpingMode.Y:
                    value.y = _lerpPos.y;
                    break;
            }
        }

        public void StopLerp()
        {
            Stop(this, "_lerpPos");
            OnMoveLerpCompleted(null, null);
        }

        private void OnMoveLerpStarted(Object obj, NodePath key) => IsLerping = true;

        private void OnMoveLerpCompleted(Object obj, NodePath key)
        {
            RemoveAll();
            Vector2? parentVelocity = (Vector2?) _connectedNode.Get("Velocity");
            switch (CurLerpingMode)
            {
                case LerpingMode.Vector2:
                    _velocity = Vector2.Zero;
                    _connectedNode.Set("Velocity", Vector2.Zero);
                    break;
                case LerpingMode.X:
                    _velocity.x = 0;
                    _connectedNode.Set("Velocity", new Vector2(0, parentVelocity.Value.y));
                    break;
                case LerpingMode.Y:
                    _velocity.y = 0;
                    _connectedNode.Set("Velocity", new Vector2(parentVelocity.Value.x, 0));
                    break;
            }

            IsLerping = false;
        }
    }
}
