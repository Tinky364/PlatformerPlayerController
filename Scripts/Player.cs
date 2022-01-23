using Godot;
using Godot.Collections;

public class Player : KinematicBody2D
{
    [Export] private float _gravity = 900f;
    [Export] private float _groundDetectionHeight = 0.1f;
    [Export] private float _moveAcceleration = 400f;
    [Export] private float _moveSpeed = 60f;
    [Export] private float _jumpHeightMin = 8f;
    [Export] private float _jumpHeightMax = 26f;
    [Export] private float _jumpInitialSpeedX = 80f;
    [Export] private float _climbDuration = 0.25f;
    [Export] private float _movementLengthAfterClimb = 8f;
    [Export] private float _edgeDetectionHeight = 12f;

    private AnimatedSprite _animatedSprite;
    private Timer _jumpTimer;
    private Physics2DDirectSpaceState _spaceState;

    private Vector2 _inputAxis;
    private Vector2 _velocity;

    private float _desiredMove;
    private float _desiredJumpX;
    private float JumpInitialSpeedY => Mathf.Sqrt(2f * _gravity * _jumpHeightMin); // V=sqrt{2*g*h}
    private float JumpAccelerationY => _gravity - Mathf.Pow(JumpInitialSpeedY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
    private float JumpSecond => JumpInitialSpeedY / (_gravity - JumpAccelerationY); // t=V/(g-a)
    private float ClimbSpeedY => _edgeDetectionHeight / _climbDuration;
    private float ClimbSpeedX => Mathf.Sqrt(2f * _moveAcceleration * _movementLengthAfterClimb);
    private int _direction;
    
    private bool _isOnGround;
    private bool _isEdgeDetected;
    private bool _isHangingOnEdge;
    private bool _isClimbing;
    private bool _isJumping;
    private bool _groundRayCastEnabled = true;

    private Dictionary _groundRaycastLeft;
    private Dictionary _groundRaycastRight;
    private Dictionary _edgeRaycast;

    public override void _Ready()
    {
        _spaceState = GetWorld2d().DirectSpaceState;

        _animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");

        _jumpTimer = new Timer();
        AddChild(_jumpTimer);
        _jumpTimer.Connect("timeout", this, nameof(OnJumpTimeout));
    }

    public override void _Process(float delta)
    {
        AxisInputs();
        JumpInput();
        ClimbInput();
        AnimationControl();
    }

    public override void _PhysicsProcess(float delta)
    {
        CheckGround();
        CheckEdge();
        CalculateVelocity(delta);
        _velocity = MoveAndSlide(_velocity, Vector2.Up);
    }
    
    private void AxisInputs()
    {
        _inputAxis.x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        _inputAxis.y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
        _desiredMove = _moveSpeed * _inputAxis.x;
        _desiredJumpX = _jumpInitialSpeedX * _inputAxis.x;
        if (_inputAxis.x > 0)
            _direction = 1;
        else if (_inputAxis.x < 0)
            _direction = -1;
    }

    private void ClimbInput()
    {
        if (_isHangingOnEdge && Input.IsActionJustPressed("move_up"))
            _isClimbing = true;
        if (_isHangingOnEdge && Input.IsActionJustPressed("move_down"))
        {
            _isHangingOnEdge = false;
            _direction *= -1;
        }
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
            if (_direction == -1)
            {
                _animatedSprite.FlipH = true;
            }
            else if (_direction == 1)
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
        if (_isHangingOnEdge)
        {
            _velocity.y = 0;

            if (_isClimbing)
            {
                _velocity.y = -ClimbSpeedY;
                _velocity.x = _direction * ClimbSpeedX;
            }
            return;
        }
        
        if (_isOnGround) // while the player is on the ground.
        {
            // while the player is walking on the ground.
            _velocity.x = Mathf.MoveToward(_velocity.x, _desiredMove, _moveAcceleration * delta);
            _velocity.y = 0f; // velocity.y must be 0 when player is on the ground.

            // when the player starts jumping.
            if (_isJumping)
            {
                _groundRayCastEnabled = true;
                _velocity.x = _desiredJumpX;
                _velocity.y = -JumpInitialSpeedY;
            }
        }
        else // while the player is in air.
        {
            _velocity.x = Mathf.MoveToward(_velocity.x, _desiredJumpX, _moveAcceleration * delta);
            _velocity.y += _gravity * delta; // adds gravity force increasingly.
            
            // while the player keep pressing the jump button.
            if (_isJumping)
                _velocity.y -= JumpAccelerationY * delta;
        }
    }

    private void CheckGround()
    {
        if (!_groundRayCastEnabled) return;
        
        _groundRaycastLeft = _spaceState.IntersectRay(
            Position + new Vector2(-4, -3), 
            Position + new Vector2(-4, _groundDetectionHeight), 
            new Array {this},
            CollisionMask,
            true,
            true
        );
        
        _groundRaycastRight = _spaceState.IntersectRay(
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
        if (_isJumping) return;
        
        // checks whether there is a wall in front of the player.
        _edgeRaycast = _spaceState.IntersectRay(
            Position + new Vector2(0, -_edgeDetectionHeight),
            Position + new Vector2(_direction * 6, -_edgeDetectionHeight),
            new Array {this},
            CollisionMask,
            true,
            true
        );
        // if there is a wall in front of the player, does not check for an edge.
        if (_edgeRaycast.Count > 0)
        {
            _isEdgeDetected = false;
            return;
        }
        
        // checks whether there is an edge.
        _edgeRaycast = _spaceState.IntersectRay(
            Position + new Vector2(_direction * 6, -_edgeDetectionHeight),
            Position + new Vector2(_direction * 6, -_edgeDetectionHeight + 2f),
            new Array {this},
            CollisionMask,
            true,
            true
        );
        _isEdgeDetected = _edgeRaycast.Count > 0;
        
        // if there is an edge, the player climbs.
        if (_isEdgeDetected) 
            _isHangingOnEdge = true;

        if (_isHangingOnEdge)
        {
            // checks the edge while the player climbs
            _edgeRaycast = _spaceState.IntersectRay(
                Position + new Vector2(0, 1f),
                Position + new Vector2(_direction * 6, 1f),
                new Array {this},
                CollisionMask,
                true,
                true
            );
            _isHangingOnEdge = _edgeRaycast.Count > 0;
        }
        else
            _isClimbing = false;
    }
}
