using Godot;
using Manager;

namespace NavTool
{
    public class NavTween : Tween
    {
        private Node2D _conNode;

        [Signal]
        private delegate void MoveStarted();
        [Signal]
        private delegate void MoveCompleted();
        
        public enum TweenMode { Vector2, X, Y }
        public TweenMode CurTweenMode { get; private set; }
        public Vector2 Pos => _pos;
        public Vector2 Velocity => _velocity;
        public bool IsPlaying { get; private set; }
        public bool IsTweenConnected { get; private set; }
        public bool IsVelocityConnected { get; private set; }
        
        private Vector2 _pos;
        private Vector2 _velocity;
        private string _velocityVar;

        public override void _Ready()
        {
            Name = "NavTween";
            PlaybackProcessMode = TweenProcessMode.Physics;
            Connect("tween_started", this, nameof(OnTweenStarted));
            Connect("tween_completed", this, nameof(OnTweenCompleted));
        }

        public override void _PhysicsProcess(float delta)
        {
            if (!IsTweenConnected)
            {
                GD.PushWarning("NavTween is not connected!");
            }
        }

        public void ConnectTween(Node2D connectedNode, string velocityVariableName = "")
        {
            if (connectedNode == null) return;
            _conNode = connectedNode;
            IsTweenConnected = true;
            if (_conNode.Get(velocityVariableName) == null) return;
            _velocityVar = velocityVariableName;
            IsVelocityConnected = true;
        }
        
        public async void MoveLerp(TweenMode mode, Vector2? initialPos, Vector2 targetPos, float duration,
                             TransitionType transitionType = TransitionType.Linear,
                             EaseType easeType = EaseType.InOut, float delay = 0f)
        {
            if (delay > 0) await TreeTimer.S.Wait(delay);
            if (IsPlaying)
            {
                GD.Print("Already lerping wait until finish or call StopMove method!");
                return;
            }
            _pos = initialPos ?? _conNode.GlobalPosition;
            CurTweenMode = mode;
            switch (CurTweenMode)
            {
                case TweenMode.X:
                    targetPos.y = _pos.y;
                    break;
                case TweenMode.Y:
                    targetPos.x = _pos.x;
                    break;
            }
            InterpolateProperty(this, "_pos", _pos, targetPos, duration, transitionType, easeType);
            Start();
        }
        
        public async void MoveToward(TweenMode mode, Vector2? initialPos, Vector2 targetPos, float speed, 
                               TransitionType transitionType = TransitionType.Linear,
                               EaseType easeType = EaseType.InOut, float delay = 0f)
        {
            if (delay != 0) await TreeTimer.S.Wait(delay);
            if (IsPlaying)
            {
                GD.Print("Already lerping wait until finish or call StopMove method!");
                return;
            }
            _pos = initialPos ?? _conNode.GlobalPosition;
            CurTweenMode = mode;
            switch (CurTweenMode)
            {
                case TweenMode.X:
                    targetPos.y = _pos.y;
                    break;
                case TweenMode.Y:
                    targetPos.x = _pos.x;
                    break;
            }
            float duration = _pos.DistanceTo(targetPos) / speed;
            InterpolateProperty(this, "_pos", _pos, targetPos, duration, transitionType, easeType);
            Start();
        }
        
        public Vector2 EqualizeVelocity(Vector2 velocity, float delta)
        {
            if (!IsPlaying || !IsVelocityConnected) return velocity;
            _velocity = _conNode.GlobalPosition.DirectionTo(_pos)
                * _conNode.GlobalPosition.DistanceTo(_pos) / delta;
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
            OnMoveCompleted();
            EmitSignal(nameof(MoveCompleted));
        }

        private void OnTweenStarted(Object obj, NodePath key)
        {
            OnMoveStarted();
            EmitSignal(nameof(MoveStarted));
        }
        private void OnTweenCompleted(Object obj, NodePath key)
        {
            OnMoveCompleted();
            EmitSignal(nameof(MoveCompleted));
        }
        
        private void OnMoveStarted()
        {
            IsPlaying = true;
        }
        
        private void OnMoveCompleted()
        {
            RemoveAll();
            if (IsVelocityConnected)
            {
                Vector2? parentVelocity = (Vector2?) _conNode.Get(_velocityVar);
                switch (CurTweenMode)
                {
                    case TweenMode.Vector2:
                        _velocity = Vector2.Zero;
                        _conNode.Set(_velocityVar, Vector2.Zero);
                        break;
                    case TweenMode.X:
                        _velocity.x = 0;
                        if (parentVelocity.HasValue)
                            _conNode.Set(_velocityVar, new Vector2(0, parentVelocity.Value.y));
                        break;
                    case TweenMode.Y:
                        _velocity.y = 0;
                        if (parentVelocity.HasValue)
                            _conNode.Set(_velocityVar, new Vector2(parentVelocity.Value.x, 0));
                        break;
                }
            }
            IsPlaying = false;
        }
    }
}
