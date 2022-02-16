using Godot;
using AI;

namespace PlayerStateMachine
{
    public class Player : KinematicBody2D
    {
        public StateMachine<PlayerStates> Fsm { get; } = new StateMachine<PlayerStates>();
        public enum PlayerStates { Move, Fall, Jump }
        
        public AnimatedSprite AnimSprite { get; private set; }
        public Timer JumpTimer { get; private set; }

        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity { get; private set; } = 1100f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        public float GravitySpeedMax { get; private set; } = 225f;
        
        [Export]
        private MoveState _moveState;
        [Export]
        private FallState _fallState;
        [Export]
        private JumpState _jumpState;
    
        private Vector2 _inputAxis;
        public Vector2 InputAxis => _inputAxis;
        public Vector2 Velocity;
        public Vector2 Direction;
        
        public override void _Ready()
        {
            AnimSprite = GetNode<AnimatedSprite>("AnimatedSprite");
            JumpTimer = GetNode<Timer>("JumpTimer");
            _moveState.Initialize(this);
            _fallState.Initialize(this);
            _jumpState.Initialize(this);
            
            JumpTimer.Connect("timeout", _jumpState, "OnJumpEnd");
            
            Fsm.SetCurrentState(PlayerStates.Fall);
        }

        public override void _Process(float delta)
        {
            Fsm._Process(delta);
            AnimationController();
        }

        public override void _PhysicsProcess(float delta)
        {
            Fsm._PhysicsProcess(delta);
        }

        public void AxisInputs()
        {
            _inputAxis.x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            _inputAxis.y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
        }
        
        private void AnimationController()
        {
            if (_inputAxis.x > 0) Direction.x = 1;
            else if (_inputAxis.x < 0) Direction.x = -1;

            switch (Direction.x)
            {
                case 1:
                    AnimSprite.FlipH = false;
                    break;
                case -1:
                    AnimSprite.FlipH = true;
                    break;
            }

            if (Fsm.CurrentState.Key == PlayerStates.Move)
                AnimSprite.Play(Velocity.x == 0 ? "idle" : "run");
            else
                AnimSprite.Play("jump");
        }
    }
}

