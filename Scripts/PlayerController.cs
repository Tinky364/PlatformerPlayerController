using Godot;
using Godot.Collections;

namespace PlatformerPlayerController.Scripts
{
    public class PlayerController : KinematicBody2D
    {
        protected CollisionShape2D Shape;
        protected AnimatedSprite AnimatedSprite;
        private Timer _jumpTimer;
        
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        private float _gravity = 1200f;
        [Export(PropertyHint.Range, "0.1,20,or_greater")] 
        private float _groundRayLength = 5f;
        [Export(PropertyHint.Range, "0.1,20,or_greater")] 
        private float _isOnGroundDetectionLength = 0.1f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _moveAcceleration = 400f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _moveSpeed = 60f;
        [Export(PropertyHint.Range, "0.01f,5,or_greater")] 
        private float _canJumpSec = 0.15f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _jumpAccelerationX = 600f;
        [Export(PropertyHint.Range, "1,50,or_greater")]
        private float _jumpHeightMin = 10f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _jumpHeightMax = 33f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _jumpWidthMax = 40f;
        [Export(PropertyHint.Range, "1,40,or_greater")] 
        private float _edgeAxisXRayLength = 3f;
        [Export(PropertyHint.Range, "1,40,or_greater")] 
        private float _edgeAxisYRayLength = 3f;
        [Export(PropertyHint.Range, "0.2f,4,or_greater")]
        private float _climbDuration = 0.25f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _snapSpeed = 100f;

        private Dictionary _groundRay;
        private Dictionary _edgeRay;
        private Vector2 _inputAxis;
        public Vector2 Velocity { get; private set; }
        public Vector2 ShapeExtents { get; private set; }
        public Vector2 ShapeSizes => ShapeExtents * 2f;
        private Vector2 _groundHitPos = new Vector2();
        private Vector2 _edgeHitPos = new Vector2();
        public int Direction { get; private set; } = 1;
        private float _desiredMove;
        private float _desiredJumpSpeedX;
        private float JumpInitialSpeedY => Mathf.Sqrt(2f * _gravity * _jumpHeightMin); // V=sqrt{2*g*h}
        private float JumpAccelerationY => _gravity - Mathf.Pow(JumpInitialSpeedY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
        private float JumpSecond => JumpInitialSpeedY / (_gravity - JumpAccelerationY); // t=V/(g-a)
        private float FallSecond => Mathf.Sqrt((2f * _jumpHeightMax) / _gravity); // t=sqrt{(2*h)/g}
        private float JumpSpeedX => _jumpWidthMax / (JumpSecond + FallSecond); // v=w/t
        private float ClimbSpeedY => ShapeSizes.y / _climbDuration; // v=w/t
        private float ClimbSpeedX => Mathf.Sqrt(2f * _moveAcceleration * ShapeSizes.x); // v=sqrt{2*a*h}
        private bool _isDroppingFromPlatformInput;
        private bool _isOnGround;
        private bool _isOnPlatform;
        private bool _isHangingOnEdge;
        private bool _isClimbingInput;
        private bool _isJumpingInput;
        private bool _hasJumpingStarted;
        private bool _hasJumpingEnded = true;
        private bool _hasGroundRayDisabled;
        private bool _hasSnapDisabled;
        private bool _canJump;
        private bool _canJumpFlag;

        public override void _Ready()
        {
            AnimatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
            
            Shape = GetNode<CollisionShape2D>("Shape");
            if (Shape.Shape is RectangleShape2D shape)
                ShapeExtents = shape.Extents;
            
            _jumpTimer = new Timer();
            AddChild(_jumpTimer);
            _jumpTimer.Name = "JumpTimer";
            _jumpTimer.Connect("timeout", this, nameof(OnJumpEnd));
        }

        public override void _Process(float delta)
        {
            AxisInputs();
            JumpInput();
            ClimbInput();
            DropFromPlatformInput();
            AnimationControl();
            
            Update();
        }

        public override void _PhysicsProcess(float delta)
        {
            CheckGround();
            CheckEdge();
            CheckCanJump();
            Velocity = CalculateMotionVelocity(delta) + CalculateSnapVelocity();
            Velocity = MoveAndSlide(Velocity, Vector2.Up);
        }
    
        private void AxisInputs()
        {
            _inputAxis.x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            _inputAxis.y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
            _desiredMove = _moveSpeed * _inputAxis.x;
            _desiredJumpSpeedX = JumpSpeedX * _inputAxis.x;
        }

        private void DropFromPlatformInput()
        {
            if (_isOnPlatform && Input.IsActionJustPressed("move_down"))
                _isDroppingFromPlatformInput = true;
        }

        private void ClimbInput()
        {
            if (_isHangingOnEdge && Input.IsActionJustPressed("move_up"))
                _isClimbingInput = true;
            if (_isHangingOnEdge && Input.IsActionJustPressed("move_down"))
            {
                _isHangingOnEdge = false;
                Direction *= -1;
            }
        }
        
        private void JumpInput()
        {
            if (_canJump && Input.IsActionJustPressed("jump"))
            {
                _hasGroundRayDisabled = true;
                _hasJumpingStarted = true;
                _isJumpingInput = true;
                _jumpTimer.Start(JumpSecond);
            }

            if (Input.IsActionJustReleased("jump"))
            {
                OnJumpEnd();
            }
        }

        private void OnJumpEnd()
        {
            if (_hasJumpingEnded) return;
            
            _isJumpingInput = false;
            _hasJumpingEnded = true;
            _jumpTimer.Stop();
        }

        private Vector2 CalculateSnapVelocity()
        {
            if (_hasSnapDisabled) return Vector2.Zero;

            // Snap while the player moves on the ground.
            if (_isOnGround)
            {
                float dif = _groundHitPos.y - Position.y;
                if (Mathf.Abs(dif) > 0.001f)
                    return Mathf.Sign(dif) * Vector2.Down * _snapSpeed;
                return Vector2.Zero;
            }
            
            // Snap while the player hangs on the edge.
            if (_isHangingOnEdge)
            {
                Vector2 difVec = new Vector2(_edgeHitPos.x - (Position.x + Direction * ShapeExtents.x),
                                             _edgeHitPos.y - (Position.y - ShapeSizes.y));
                if (difVec.Length() > 0.001f)
                    return difVec.Normalized() * _snapSpeed;
                return Vector2.Zero;
            }
            
            return Vector2.Zero;
        }
        private Vector2 CalculateMotionVelocity(float delta)
        {
            Vector2 velocity = Velocity;
            
            // While the player is hanging on the edge.
            if (_isHangingOnEdge)
            {
                velocity.x = 0f;
                // While the player wants to climb.
                if (_isClimbingInput)
                {
                    velocity.y = -ClimbSpeedY;
                    _hasSnapDisabled = true;
                    return velocity;
                }
                velocity.y = 0f;
                return velocity;
            }

            // First frame after the edge is crossed.
            if (_isClimbingInput)
            {
                _isClimbingInput = false;
                _hasSnapDisabled = false;
                velocity.x = Direction * ClimbSpeedX;
                return velocity;
            }

            // While the player is on the ground.
            if (_isOnGround) 
            {
                // First frame when the player is on the ground.
                OnJumpEnd();
                
                // First frame when the player starts jumping.
                if (_canJump && _hasJumpingStarted)
                {
                    _canJump = false;
                    _hasJumpingEnded = false;
                    _isOnGround = false;
                    velocity.x = _desiredJumpSpeedX;
                    velocity.y = -JumpInitialSpeedY;
                    return velocity;
                }
                
                // First frame when the player starts dropping from a platform.
                if (_isDroppingFromPlatformInput)
                {
                    _isOnGround = false;
                    SetCollisionMaskBit(2, false); // Layer 3
                    return velocity;
                }
                
                // While the player is walking on the ground.
                velocity.x = Mathf.MoveToward(velocity.x, _desiredMove, _moveAcceleration * delta);
                velocity.y = 0f; 
                return velocity;
            }
            
            // While the player is in the air.
            
            if (_canJump)
            {
                // First frame when the player starts jumping.
                if (_hasJumpingStarted)
                {
                    _canJump = false;
                    _hasJumpingEnded = false;
                    _isOnGround = false;
                    velocity.x = _desiredJumpSpeedX;
                    velocity.y = -JumpInitialSpeedY;
                    return velocity;
                }
            }
            else
            {
                // Second frame when the player starts jumping.
                if (_hasJumpingStarted)
                {
                    _hasJumpingStarted = false;
                    _hasGroundRayDisabled = false;
                }
            }
           
            // While the player keep pressing the jump button in the air.
            if (_isJumpingInput)
            {
                if (Velocity.y > 0f)
                    OnJumpEnd();
                else
                    velocity.y -= JumpAccelerationY * delta;
            }
            
            velocity.x = Mathf.MoveToward(velocity.x, _desiredJumpSpeedX, _jumpAccelerationX * delta);
            velocity.y += _gravity * delta; // Adds gravity force increasingly.
            return velocity;
        }

        private void CheckCanJump()
        {
            if (_isHangingOnEdge)
            {
                _canJump = false;
                return;
            }

            if (!_hasJumpingEnded)
            {
                _canJump = false;
                return;
            }

            if (_isOnGround)
            {
                _canJump = true;
                _canJumpFlag = true;
                return;
            }
            
            if (_groundRay.Count <= 0)
            {
                if (_canJumpFlag)
                {
                    _canJumpFlag = false;
                    CanJumpTimer();
                }
            }
            else
            {
                _canJump = true;
            }
        }

        private async void CanJumpTimer()
        {
            await ToSignal(GetTree().CreateTimer(_canJumpSec), "timeout");
            _canJump = false;
        }
        private void CheckGround()
        {
            if (_hasGroundRayDisabled) return;

            // Raycast from the left bottom corner of the player.
            _groundRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(-ShapeExtents.x, -1f), 
                Position + new Vector2(-ShapeExtents.x, _groundRayLength), 
                new Array {this},
                CollisionMask
            );
            if (_groundRay.Count > 0 && IsGroundAngleEnough(CalculateGroundAngle((Vector2) _groundRay["normal"]), 5f))
            {
                _groundHitPos = (Vector2) _groundRay["position"] + new Vector2(ShapeExtents.x, 0);
                if (_groundHitPos.DistanceTo(GlobalPosition) < _isOnGroundDetectionLength)
                {
                    _isOnGround = true;
                    CheckPlatform(_groundRay["collider"] as CollisionObject2D);
                }
                return;
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner of the player.
            _groundRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(ShapeExtents.x, -1f),
                Position + new Vector2(ShapeExtents.x, _groundRayLength),
                new Array {this},
                CollisionMask
            );
            if (_groundRay.Count > 0 && IsGroundAngleEnough(CalculateGroundAngle((Vector2) _groundRay["normal"]), 5f))
            {
                _groundHitPos = (Vector2) _groundRay["position"] + new Vector2(-ShapeExtents.x, 0);
                if (_groundHitPos.DistanceTo(GlobalPosition) < _isOnGroundDetectionLength)
                {
                    _isOnGround = true;
                    CheckPlatform(_groundRay["collider"] as CollisionObject2D);
                }
                return;
            }
            // If raycasts do not hit the ground.
            _isOnGround = false;
            _isOnPlatform = false;
        }

        private bool IsGroundAngleEnough(float groundAngle, float limit) => groundAngle > -limit && groundAngle < limit;
        
        private float CalculateGroundAngle(Vector2 normal) => Mathf.Rad2Deg(normal.AngleTo(Vector2.Up));

        private void CheckPlatform(CollisionObject2D body)
        {
            if (body == null) return;
            foreach (int id in body.GetShapeOwners())
                _isOnPlatform = body.IsShapeOwnerOneWayCollisionEnabled((uint)id);
        }

        private void CheckEdge()
        {
            if (_isOnGround) return;
            if (_isJumpingInput) return;

            float rayPosX = ShapeExtents.x + _edgeAxisXRayLength;
            float rayPosY = -ShapeSizes.y - _edgeAxisYRayLength;
            // Checks whether there are inner collisions.
            _edgeRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(Direction * ShapeExtents.x, -ShapeSizes.y),
                Position + new Vector2(Direction * ShapeExtents.x, -ShapeSizes.y + 2f),
                new Array {this},
                CollisionMask
            );
            // If there is an inner collision, does not check for a wall.
            if (_edgeRay.Count > 0) return;
            
            // Checks whether there is a wall in front of the player.
            _edgeRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(Direction * ShapeExtents.x, rayPosY),
                Position + new Vector2(Direction * rayPosX, rayPosY),
                new Array {this},
                CollisionMask
            );
            // If there is a wall in front of the player, does not check for an edge.
            if (_edgeRay.Count > 0) return;
        
            // Checks whether there is an edge.
            _edgeRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(Direction * rayPosX, rayPosY),
                Position + new Vector2(Direction * rayPosX, -ShapeSizes.y),
                new Array {this},
                CollisionMask
            );
            // If there is an edge, the player starts hanging on the edge.
            if (_edgeRay.Count > 0)
            {
                _isHangingOnEdge = true;
                _edgeHitPos = (Vector2) _edgeRay["position"];
            }
            
            // If the player is not hanging on the edge yet, does not check the wall. 
            if (!_isHangingOnEdge) return;
            
            // Checks the wall from the player`s feet while the player hangs on the edge.
            _edgeRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(Direction * ShapeExtents.x, 0f),
                Position + new Vector2(Direction * rayPosX, 0f),
                new Array {this},
                CollisionMask
            );
            _isHangingOnEdge = _edgeRay.Count > 0;
        }
        
        private void AnimationControl()
        {
            if (_inputAxis.x > 0)
                Direction = 1;
            else if (_inputAxis.x < 0)
                Direction = -1;
            
            switch (Direction)
            {
                case 1:
                    AnimatedSprite.FlipH = false;
                    break;
                case -1:
                    AnimatedSprite.FlipH = true;
                    break;
            }
            
            if (_isOnGround)
                AnimatedSprite.Play(Velocity.x == 0 ? "idle" : "run");
            else
                AnimatedSprite.Play("jump");
        }

        private void OnPlatformBodyEntered(Node body) => _hasGroundRayDisabled = true;
        
        private void OnPlatformBodyExited(Node body)
        {
            _hasGroundRayDisabled = false;
            if (!_isDroppingFromPlatformInput) return;
            
            _isDroppingFromPlatformInput = false;
            SetCollisionMaskBit(2, true); // Layer 3
        }

        public override void _Draw()
        {
            float rayPosX = ShapeExtents.x + _edgeAxisXRayLength;
            float rayPosY = -ShapeSizes.y - _edgeAxisYRayLength;
            
           
            DrawLine(Vector2.Zero,
                     Vector2.Down * _groundRayLength,
                     _isOnGround ? Colors.Green : Colors.Red
            );
            DrawLine(new Vector2(Direction * ShapeExtents.x, rayPosY),
                     new Vector2(Direction * rayPosX, rayPosY),
                     Colors.Red
            );
            DrawLine(new Vector2(Direction * rayPosX, rayPosY),
                     new Vector2(Direction * rayPosX, -ShapeSizes.y),
                     _isHangingOnEdge ? Colors.Green : Colors.Red
            );
        }
    }
}
