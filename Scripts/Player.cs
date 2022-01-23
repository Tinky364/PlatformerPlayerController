using Godot;
using Godot.Collections;

public class Player : KinematicBody2D
{
    [Export] private float _gravity = 900f;
    [Export] private float _moveAcceleration = 400f;
    [Export] private float _moveSpeed = 60f;
    [Export] private float _jumpHeightMin = 8f;
    [Export] private float _jumpHeightMax = 26f;
    [Export] private float _jumpInitialSpeedX = 80f;
    [Export] private float _groundDetectionHeight = 0.1f;

    private AnimatedSprite _animatedSprite;
    private Timer _jumpTimer;

    private Vector2 _inputAxis;
    private Vector2 _velocity;

    private float _desiredMoveSpeed;
    private float _desiredJumpSpeedX;
    private float JumpInitialSpeedY => Mathf.Sqrt(2f * _gravity * _jumpHeightMin); // V=sqrt{2*g*h}
    private float JumpAccelerationY => _gravity - Mathf.Pow(JumpInitialSpeedY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
    private float JumpSecond => JumpInitialSpeedY / (_gravity - JumpAccelerationY); // t=V/(g-a)

    private bool _isOnGround;
    private bool _isOnEdge;
    private bool _isJumpStarted;
    private bool _isJumping;
    private bool _groundRayCastEnabled = true;

    private Dictionary _groundRaycastLeft;
    private Dictionary _groundRaycastRight;
    private Dictionary _edgeRaycast;

    public override void _Ready()
    {
        _animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");

        _jumpTimer = new Timer();
        AddChild(_jumpTimer);
        _jumpTimer.Connect("timeout", this, nameof(OnJumpTimeout));
    }

    public override void _Process(float delta)
    {
        AxisInputX();
        JumpInput();
        AnimationControl();
    }

    public override void _PhysicsProcess(float delta)
    {
        CheckGround();
        CheckEdge();
        CalculateVelocity(delta);
        _velocity = MoveAndSlide(_velocity, Vector2.Up);
    }

    private void AxisInputX()
    {
        _inputAxis.x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        _inputAxis = _inputAxis.Normalized();
        _desiredMoveSpeed = _moveSpeed * _inputAxis.x;
        _desiredJumpSpeedX = _jumpInitialSpeedX * _inputAxis.x;
    }

    private void AnimationControl()
    {
        if (_isOnGround)
        {
            if (_velocity.x < 0)
            {
                _animatedSprite.FlipH = true;
                _animatedSprite.Play("run");
            }
            else if (_velocity.x > 0)
            {
                _animatedSprite.FlipH = false;
                _animatedSprite.Play("run");
            }
            else
            {
                _animatedSprite.Play("idle");
                _animatedSprite.Frame = 1;
            }
        }
        else
        {
            _animatedSprite.Play("jump");
            if (_velocity.x < 0)
            {
                _animatedSprite.FlipH = true;
            }
            else if (_velocity.x > 0)
            {
                _animatedSprite.FlipH = false;
            }
        }
    }

    private void JumpInput()
    {
        if (_isOnGround && Input.IsActionJustPressed("jump"))
        {
            _groundRayCastEnabled = false;
            _isJumpStarted = true;
            _isJumping = true;
            _jumpTimer.Start(JumpSecond);
        }

        if (Input.IsActionJustReleased("jump"))
            _isJumping = false;
    }

    private void OnJumpTimeout()
    {
        _isJumping = false;
        _jumpTimer.Stop();
    }

    private void CalculateVelocity(float delta)
    {
        if (_isOnGround) // while the player is on the ground.
        {
            // while the player is walking on the ground.
            _velocity.x = Mathf.MoveToward(_velocity.x, _desiredMoveSpeed, _moveAcceleration * delta);
            _velocity.y = 0f; // velocity.y must be 0 when player is on the ground.

            // when the player starts jumping.
            if (!_isJumpStarted) return; // jumps if the jump button is pressed.
            _isJumpStarted = false;
            _groundRayCastEnabled = true;
            _velocity.x = _desiredJumpSpeedX;
            _velocity.y = -JumpInitialSpeedY;
        }
        else // while the player is in air.
        {
            if (_isJumping)
                _velocity.y -= JumpAccelerationY * delta;
            _velocity.x = Mathf.MoveToward(_velocity.x, _desiredJumpSpeedX, _moveAcceleration * delta);
            _velocity.y += _gravity * delta; // adds gravity force increasingly.
        }
    }

    private void CheckGround()
    {
        if (!_groundRayCastEnabled) return;
        var spaceState = GetWorld2d().DirectSpaceState;
        _groundRaycastLeft = spaceState.IntersectRay(
            Position + new Vector2(-4, -3), 
            Position + new Vector2(-4, _groundDetectionHeight), 
            new Array {this},
            CollisionMask,
            true,
            true
        );
        _groundRaycastRight = spaceState.IntersectRay(
            Position + new Vector2(4, -3),
            Position + new Vector2(4, _groundDetectionHeight),
            new Array {this},
            CollisionMask,
            true,
            true
        );
        _isOnGround = _groundRaycastLeft.Count > 0 || _groundRaycastRight.Count > 0;
    }

    private void CheckEdge()
    {
        if (_isOnGround) return;
        var spaceState = GetWorld2d().DirectSpaceState;
        _edgeRaycast = spaceState.IntersectRay(
            Position + new Vector2(10, -14),
            Position + new Vector2(10, -10),
            new Array {this},
            CollisionMask,
            true,
            true
        );
        _isOnEdge = _edgeRaycast.Count > 0;
        if (_isOnEdge)
            GD.Print("Edge Detected");
    }
}
