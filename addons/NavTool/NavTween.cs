using Godot;

namespace NavTool
{
    public class NavTween : Tween
    {
        private Node2D _connectedNode;

        [Signal]
        private delegate void MoveStarted();
        [Signal]
        private delegate void MoveCompleted();
        
        public enum TweenMode { Vector2, X, Y }
        public TweenMode CurTweenMode { get; private set; }
        private Vector2 _pos;
        public Vector2 Pos => _pos;
        private Vector2 _velocity;
        public Vector2 Velocity => _velocity;
        private Vector2 _moveTowardTargetPos;
        private float _moveTowardSpeed;
        public bool IsPlaying { get; private set; }
        private bool _isTowarding;
        private string _velocityVar;
        public bool IsVelocityVarConnected { get; private set; }

        public override void _EnterTree()
        {
            _connectedNode = GetParent<Node2D>();
        }

        public override void _Ready()
        {
            PlaybackProcessMode = TweenProcessMode.Physics;
            Connect("tween_started", this, nameof(OnTweenStarted));
            Connect("tween_completed", this, nameof(OnTweenCompleted));
            Connect("MoveStarted", this, nameof(OnMoveStarted));
            Connect("MoveCompleted", this, nameof(OnMoveCompleted));
        }

        public override void _PhysicsProcess(float delta)
        {
            CalculateLerpVelocity(delta);
            CalculateMoveTowards(delta);
        }

        public void ConnectVelocityVariable(string velocityVariableName)
        {
            _velocityVar = velocityVariableName;
            if (_connectedNode.Get(_velocityVar) != null)
                IsVelocityVarConnected = true;
        }

        private void CalculateLerpVelocity(float delta)
        {
            if (!IsPlaying) return;
            _velocity = _connectedNode.GlobalPosition.DirectionTo(_pos) * _connectedNode.GlobalPosition.DistanceTo(_pos) / delta;
        }
        
        public void MoveLerp(
            TweenMode mode,
            Vector2? initialPos,
            Vector2 targetPos,
            float duration,
            TransitionType transitionType = TransitionType.Linear,
            EaseType easeType = EaseType.InOut,
            float delay = 0f)
        {
            if (IsPlaying)
            {
                GD.Print("Already lerping wait until finish or call StopMove method!");
                return;
            }
            _pos = initialPos ?? _connectedNode.GlobalPosition;
            switch (CurTweenMode)
            {
                case TweenMode.X:
                    targetPos.y = _pos.y;
                    break;
                case TweenMode.Y:
                    targetPos.x = _pos.x;
                    break;
            }
            CurTweenMode = mode;
            InterpolateProperty(this, "_pos", _pos, targetPos, duration, transitionType, easeType, delay);
            Start();
        }

        public void MoveToward(TweenMode mode, Vector2? initialPos, Vector2 targetPos, float speed)
        {
            if (IsPlaying)
            {
                GD.Print("Already lerping wait until finish or call StopMove method!");
                return;
            }
            _pos = initialPos ?? _connectedNode.GlobalPosition;
            switch (CurTweenMode)
            {
                case TweenMode.X:
                    targetPos.y = _pos.y;
                    break;
                case TweenMode.Y:
                    targetPos.x = _pos.x;
                    break;
            }
            CurTweenMode = mode;
            EmitSignal(nameof(MoveStarted));           
            _moveTowardTargetPos = targetPos;
            _moveTowardSpeed = speed;
        }
        
        private void CalculateMoveTowards(float delta)
        {
            if (!_isTowarding) return;
            if (!IsPlaying) return;
            if (_pos != _moveTowardTargetPos)
            {
                _pos = _pos.MoveToward(_moveTowardTargetPos, _moveTowardSpeed * delta);
            }
            else
            {
                if (!_isTowarding) return;
                EmitSignal(nameof(MoveCompleted));      
            }
        }
        
        public Vector2 EqualizeVelocity(Vector2 velocity)
        {
            if (!IsPlaying) return velocity;
            switch (CurTweenMode)
            {
                case TweenMode.Vector2:
                    velocity = _velocity;
                    break;
                case TweenMode.X:
                    velocity.x = _velocity.x;
                    break;
                case TweenMode.Y:
                    velocity.y = _velocity.y;
                    break;
            }
            return velocity;
        }

        public Vector2 EqualizePosition(Vector2 position)
        {
            if (!IsPlaying) return position;
            switch (CurTweenMode)
            {
                case TweenMode.Vector2:
                    position = _pos;
                    break;
                case TweenMode.X:
                    position.x = _pos.x;
                    break;
                case TweenMode.Y:
                    position.y = _pos.y;
                    break;
            }
            return position;
        }

        public void StopMove()
        {
            Stop(this, "_pos");
            EmitSignal(nameof(MoveCompleted));
        }

        private void OnTweenStarted(Object obj, NodePath key)
        {
            EmitSignal(nameof(MoveStarted));
        }
        private void OnTweenCompleted(Object obj, NodePath key)
        {
            EmitSignal(nameof(MoveCompleted));
        }
        
        private void OnMoveStarted()
        {
            IsPlaying = true;
            _isTowarding = true;
        }
        
        private void OnMoveCompleted()
        {
            RemoveAll();
            Vector2? parentVelocity = (Vector2?) _connectedNode.Get(_velocityVar);
            switch (CurTweenMode)
            {
                case TweenMode.Vector2:
                    _velocity = Vector2.Zero;
                    _connectedNode.Set(_velocityVar, Vector2.Zero);
                    break;
                case TweenMode.X:
                    _velocity.x = 0;
                    if (parentVelocity.HasValue)
                        _connectedNode.Set(_velocityVar, new Vector2(0, parentVelocity.Value.y));
                    break;
                case TweenMode.Y:
                    _velocity.y = 0;
                    if (parentVelocity.HasValue)
                        _connectedNode.Set(_velocityVar, new Vector2(parentVelocity.Value.x, 0));
                    break;
            }
            IsPlaying = false;
            _isTowarding = false;
        }
    }
}
