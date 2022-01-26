using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts
{
    public class PlayerController : KinematicBody2D
    {
        [Export(PropertyHint.Range, "0,2000,or_greater")]
        private float _gravity = 900f;
        [Export(PropertyHint.Range, "0.1,20")]
        private float _groundDetectionHeight = 0.1f;
        [Export(PropertyHint.Range, "0,2000,or_greater")]
        private float _moveAcceleration = 400f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _moveSpeed = 60f;
        [Export(PropertyHint.Range, "0,50,or_greater")]
        private float _jumpHeightMin = 8f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _jumpHeightMax = 26f;
        [Export(PropertyHint.Range, "0,200,or_greater")]
        private float _jumpInitialSpeedX = 80f;
        [Export(PropertyHint.Range, "0,4,or_greater")]
        private float _climbDuration = 0.25f;
        [Export(PropertyHint.Range, "0,200,or_greater")] 
        private Vector2 _shapeSize = new Vector2(8, 11);
    
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
        private float ClimbSpeedY => (_shapeSize.y + 1f) / _climbDuration;
        private float ClimbSpeedX => Mathf.Sqrt(2f * _moveAcceleration * _shapeSize.x);
        private int _direction;
    
        private bool _isOnGround;
        private bool _isHangingOnEdge;
        private bool _isClimbing;
        private bool _isJumping;
        private bool _isJumpingStarted;
        private bool _groundRayCastEnabled = true;

        private Dictionary _groundRaycastLeft;
        private Dictionary _groundRaycastRight;
        private Dictionary _edgeRaycast;

        private uint RayCollisionMask => (uint) (Mathf.Pow(2, 2 - 1) + Mathf.Pow(2, 3 - 1));

        public override void _Ready()
        {
            _spaceState = GetWorld2d().DirectSpaceState;

            _animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
            
            _jumpTimer = new Timer();
            _jumpTimer.Name = "JumpTimer";
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
                    _animatedSprite.FlipH = true;
                else if (_direction == 1)
                    _animatedSprite.FlipH = false;
            }
        }

        private void JumpInput()
        {
            if (_isOnGround && Input.IsActionJustPressed("jump"))
            {
                _groundRayCastEnabled = false;
                _isJumpingStarted = true;
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
            switch (_isHangingOnEdge)
            {
                // While the player is hanging on the edge.
                case true:
                {
                    _velocity.x = 0f;
                    _velocity.y = 0f;
                    // While the player wants to climb.
                    if (_isClimbing)
                        _velocity.y = -ClimbSpeedY;
                    return;
                }
                // One frame after the edge is crossed.
                case false when _isClimbing:
                    _isClimbing = false;
                    _velocity.x = _direction * ClimbSpeedX;
                    return;
            }

            // While the player is on the ground.
            if (_isOnGround) 
            {
                // While the player is walking on the ground.
                _velocity.x = Mathf.MoveToward(_velocity.x, _desiredMove, _moveAcceleration * delta);
                _velocity.y = 0f; 
                // When the player starts jumping.
                if (_isJumpingStarted)
                {
                    _isJumpingStarted = false;
                    _groundRayCastEnabled = true;
                    _velocity.x = _desiredJumpX;
                    _velocity.y = -JumpInitialSpeedY;
                }
                return;
            }
        
            // While the player is in the air.
            _velocity.x = Mathf.MoveToward(_velocity.x, _desiredJumpX, _moveAcceleration * delta);
            _velocity.y += _gravity * delta; // Adds gravity force increasingly.
            // While the player keep pressing the jump button in the air.
            if (_isJumping)
                _velocity.y -= JumpAccelerationY * delta;
        }

        private void CheckGround()
        {
            if (!_groundRayCastEnabled) return;

            _groundRaycastLeft = _spaceState.IntersectRay(
                Position + new Vector2(-4, 0), 
                Position + new Vector2(-4, _groundDetectionHeight), 
                new Array {this},
                RayCollisionMask
            );
            
            _groundRaycastRight = _spaceState.IntersectRay(
                Position + new Vector2(4, 0),
                Position + new Vector2(4, _groundDetectionHeight),
                new Array {this},
                RayCollisionMask
            );

            if (_groundRaycastLeft.Count > 0 && (Vector2)_groundRaycastLeft["normal"] == Vector2.Up
            || _groundRaycastRight.Count > 0 && (Vector2)_groundRaycastRight["normal"] == Vector2.Up)
            {
                _isOnGround = true;
            }
            else
            {
                _isOnGround = false;
            }
        }

        private void CheckEdge()
        {
            if (_isOnGround) return;
            if (_isJumping) return;

            // Checks whether there are inner collisions.
            _edgeRaycast = _spaceState.IntersectRay(
                Position + new Vector2(_direction * (_shapeSize.x / 2f - 2f), -_shapeSize.y),
                Position + new Vector2(_direction * (_shapeSize.x / 2f - 2f), -_shapeSize.y + 2f),
                new Array {this},
                RayCollisionMask
            );
            // If there is an inner collision, does not check for a wall.
            if (_edgeRaycast.Count > 0) return;
            
            // Checks whether there is a wall in front of the player.
            _edgeRaycast = _spaceState.IntersectRay(
                Position + new Vector2(_direction * (_shapeSize.x / 2f - 1f), -_shapeSize.y - 1f),
                Position + new Vector2(_direction * (_shapeSize.x / 2f + 2f), -_shapeSize.y - 1f),
                new Array {this},
                RayCollisionMask
            );
            // If there is a wall in front of the player, does not check for an edge.
            if (_edgeRaycast.Count > 0) return;
        
            // Checks whether there is an edge.
            _edgeRaycast = _spaceState.IntersectRay(
                Position + new Vector2(_direction * (_shapeSize.x / 2f + 2f), -_shapeSize.y - 1f),
                Position + new Vector2(_direction * (_shapeSize.x / 2f + 2f), -_shapeSize.y + 1f),
                new Array {this},
                RayCollisionMask
            );
            // If there is an edge, the player starts hanging on the edge.
            if (_edgeRaycast.Count > 0) _isHangingOnEdge = true;
            
            // If the player is not hanging on the edge yet, does not check the wall. 
            if (!_isHangingOnEdge) return;
            
            // Checks the wall from the player`s feet while the player hangs on the edge.
            _edgeRaycast = _spaceState.IntersectRay(
                Position + new Vector2(_direction * (_shapeSize.x / 2f - 1f), 0f),
                Position + new Vector2(_direction * (_shapeSize.x / 2f + 2f), 0f),
                new Array {this},
                RayCollisionMask
            );
            _isHangingOnEdge = _edgeRaycast.Count > 0;
        }

    }
}
