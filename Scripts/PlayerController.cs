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
        private float _gravity = 1000f;
        [Export(PropertyHint.Range, "0.1,20")] 
        private float _groundDetectionHeight = 0.1f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _moveAcceleration = 400f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _moveSpeed = 60f;
        [Export(PropertyHint.Range, "1,50,or_greater")]
        private float _jumpHeightMin = 8f;
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _jumpHeightMax = 26f;
        [Export(PropertyHint.Range, "0,400,or_greater")]
        private float _jumpWidthMax = 32f;
        [Export(PropertyHint.Range, "0.2f,4,or_greater")]
        private float _climbDuration = 0.25f;

        private Dictionary _groundRay;
        private Dictionary _edgeRay;
        private Vector2 _inputAxis;
        public Vector2 InputAxis => _inputAxis;
        public Vector2 Velocity { get; private set; }
        public Vector2 ShapeExtents { get; private set; }
        public Vector2 ShapeSizes => ShapeExtents * 2f;
        private Vector2 _groundHitPos = new Vector2();
        private Vector2 _wallHitPos = new Vector2();
        public int Direction { get; private set; } = 1;
        private float _desiredMove;
        private float _desiredJumpSpeedX;
        private float JumpInitialSpeedY => Mathf.Sqrt(2f * _gravity * _jumpHeightMin); // V=sqrt{2*g*h}
        private float JumpAccelerationY => _gravity - Mathf.Pow(JumpInitialSpeedY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
        private float JumpSecond => JumpInitialSpeedY / (_gravity - JumpAccelerationY); // t=V/(g-a)
        private float FallSecond => Mathf.Sqrt((2f * _jumpHeightMax) / _gravity); // t=sqrt{(2*h)/g}
        private float JumpSpeedX => _jumpWidthMax / (JumpSecond + FallSecond); // v=w/t
        private float ClimbSpeedY => (ShapeSizes.y + 1f) / _climbDuration; // v=w/t
        private float ClimbSpeedX => Mathf.Sqrt(2f * _moveAcceleration * ShapeSizes.x); // v=sqrt{2*a*h}
        private bool _isDroppingFromPlatformInput;
        private bool _isOnGround;
        private bool _isOnPlatform;
        private bool _isHangingOnEdge;
        private bool _isClimbingInput;
        private bool _isJumpingInput;
        private bool _hasJumpingStarted;
        private bool _hasJumpingEnded;
        private bool _hasGroundRayEnabled = true;

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
        }

        public override void _PhysicsProcess(float delta)
        {
            CheckGround();
            CheckEdge();
            Velocity = CalculateVelocity(delta) + CalculateSnap();
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
            if (_isOnGround && Input.IsActionJustPressed("jump"))
            {
                _hasGroundRayEnabled = false;
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
            if (!_hasJumpingEnded) return;
            
            _isJumpingInput = false;
            _hasJumpingEnded = false;
            _jumpTimer.Stop();
        }

        private Vector2 CalculateSnap()
        {
            // Snap while the player hangs on the edge.
            float dif = _wallHitPos.x - (Position.x + Direction * (ShapeExtents.x));
            if (_isHangingOnEdge && Mathf.Abs(dif) > 0.001f)
                return Mathf.Sign(dif) * Vector2.Right * _moveSpeed;
            
            // Snap while the player moves on the ground.
            dif = _groundHitPos.y - Position.y;
            if (_isOnGround && Mathf.Abs(dif) > 0.001f)
                return Mathf.Sign(dif) * Vector2.Down * _moveSpeed;
            
            return Vector2.Zero;
        }
        private Vector2 CalculateVelocity(float delta)
        {
            Vector2 velocity = Velocity;
            switch (_isHangingOnEdge)
            {
                // While the player is hanging on the edge.
                case true:
                    velocity.x = 0f;
                    velocity.y = 0f;
                    // While the player wants to climb.
                    if (_isClimbingInput)
                        velocity.y = -ClimbSpeedY;
                    return velocity;
                // First frame after the edge is crossed.
                case false when _isClimbingInput:
                    _isClimbingInput = false;
                    velocity.x = Direction * ClimbSpeedX;
                    return velocity;
            }

            // While the player is on the ground.
            if (_isOnGround) 
            {
                // First frame when the player is on the ground.
                OnJumpEnd();
                
                // While the player is walking on the ground.
                velocity.x = Mathf.MoveToward(velocity.x, _desiredMove, _moveAcceleration * delta);
                velocity.y = 0f; 
                // First frame when the player starts jumping.
                if (_hasJumpingStarted)
                {
                    _hasJumpingEnded = true;
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
                return velocity;
            }

            // Second frame when the player starts jumping.
            if (_hasJumpingStarted)
            {
                _hasJumpingStarted = false;
                _hasGroundRayEnabled = true;
            }
            // While the player is in the air.
            velocity.x = Mathf.MoveToward(velocity.x, _desiredJumpSpeedX, _moveAcceleration * delta);
            velocity.y += _gravity * delta; // Adds gravity force increasingly.
            // While the player keep pressing the jump button in the air.
            if (_isJumpingInput)
                velocity.y -= JumpAccelerationY * delta;

            return velocity;
        }

        private void CheckGround()
        {
            if (!_hasGroundRayEnabled) return;

            // Raycast from the left bottom corner of the player.
            _groundRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(-ShapeExtents.x, -1f), 
                Position + new Vector2(-ShapeExtents.x, _groundDetectionHeight), 
                new Array {this},
                CollisionMask
            );
            if (_groundRay.Count > 0 && 
                IsGroundAngleEnough(CalculateGroundAngle((Vector2) _groundRay["normal"]), 5f))
            {
                _groundHitPos = (Vector2) _groundRay["position"] + new Vector2(ShapeExtents.x, 0);
                _isOnGround = true;
                CheckPlatform(_groundRay["collider"] as CollisionObject2D);
                return;
            }
            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner of the player.
            _groundRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(ShapeExtents.x, -1f),
                Position + new Vector2(ShapeExtents.x, _groundDetectionHeight),
                new Array {this},
                CollisionMask
            );
            if (_groundRay.Count > 0 && 
                IsGroundAngleEnough(CalculateGroundAngle((Vector2) _groundRay["normal"]), 5f))
            {
                _groundHitPos = (Vector2) _groundRay["position"] + new Vector2(-ShapeExtents.x, 0);
                _isOnGround = true;
                CheckPlatform(_groundRay["collider"] as CollisionObject2D);
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
                Position + new Vector2(Direction * (ShapeExtents.x - 1f), -ShapeSizes.y - 1f),
                Position + new Vector2(Direction * (ShapeExtents.x + 2f), -ShapeSizes.y - 1f),
                new Array {this},
                CollisionMask
            );
            // If there is a wall in front of the player, does not check for an edge.
            if (_edgeRay.Count > 0) return;
        
            // Checks whether there is an edge.
            _edgeRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(Direction * (ShapeExtents.x + 2f), -ShapeSizes.y - 1f),
                Position + new Vector2(Direction * (ShapeExtents.x + 2f), -ShapeSizes.y + 1f),
                new Array {this},
                CollisionMask
            );
            // If there is an edge, the player starts hanging on the edge.
            if (_edgeRay.Count > 0)
            {
                _isHangingOnEdge = true;
                _wallHitPos = (Vector2) _edgeRay["position"];
            }
            
            // If the player is not hanging on the edge yet, does not check the wall. 
            if (!_isHangingOnEdge) return;
            
            // Checks the wall from the player`s feet while the player hangs on the edge.
            _edgeRay = GetWorld2d().DirectSpaceState.IntersectRay(
                Position + new Vector2(Direction * (ShapeExtents.x - 1f), 0f),
                Position + new Vector2(Direction * (ShapeExtents.x + 2f), 0f),
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

        private void OnPlatformBodyEntered(Node body) => _hasGroundRayEnabled = false;
        
        private void OnPlatformBodyExited(Node body)
        {
            _hasGroundRayEnabled = true;
            if (!_isDroppingFromPlatformInput) return;
            
            _isDroppingFromPlatformInput = false;
            SetCollisionMaskBit(2, true); // Layer 3
        }
    }
}
