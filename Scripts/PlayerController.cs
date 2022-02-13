using Godot;
using Godot.Collections;
using NavTool;

public class PlayerController : NavBody2D
{
    protected AnimatedSprite AnimSprite;
    private Timer _jumpTimer;
    
    [Export(PropertyHint.Range, "0.1,20,0.05,or_greater")] 
    private float _isOnGroundDetectionLength = 0.1f;
    [Export(PropertyHint.Range, "1,2000,or_greater")]
    private float _moveAcceleration = 400f;
    [Export(PropertyHint.Range, "1,200,or_greater")]
    private float _moveSpeed = 60f;
    [Export(PropertyHint.Range, "0.01,5,0.05,or_greater")] 
    private float _canJumpDur = 0.1f;
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
    private float _climbDur = 0.25f;
    [Export(PropertyHint.Range, "1,200,or_greater")]
    private float _snapSpeed = 100f;
    [Export(PropertyHint.Range, "0,100,or_greater")]
    private float _recoilLength = 24f;

    private Dictionary _edgeRay;
    private Vector2 _inputAxis;
    private Vector2 _edgeHitPos;
    private Vector2 _recoilHitNormal;
    private float _desiredMove;
    private float _desiredJumpSpeedX;
    private float JumpInitialSpeedY => Mathf.Sqrt(2f * Gravity * _jumpHeightMin); // V=sqrt{2*g*h}
    private float JumpAccelerationY => Gravity - Mathf.Pow(JumpInitialSpeedY, 2) / (2 * _jumpHeightMax); // a=g-(v^2/2*h)
    private float JumpDur => JumpInitialSpeedY / (Gravity - JumpAccelerationY); // t=V/(g-a)
    private float FallDur => Mathf.Sqrt(2f * _jumpHeightMax / Gravity); // t=sqrt{(2*h)/g}
    private float JumpSpeedX => _jumpWidthMax / (JumpDur + FallDur); // v=w/t
    private float ClimbSpeedY => ShapeSizes.y / _climbDur; // v=w/t
    private float ClimbSpeedX => Mathf.Sqrt(2f * _moveAcceleration * ShapeSizes.x); // v=sqrt{2*a*w}
    private float RecoilSpeed => Mathf.Sqrt(2f * _moveAcceleration * _recoilLength); // v=sqrt{2*a*w}
    protected float RecoilDur => Mathf.Sqrt(2f * _recoilLength / _moveAcceleration); // t=sqrt{(2*w)/a}
    private bool _hasDropFromPlatformInput;
    private bool _isOnPlatform;
    private bool _isHangingOnEdge;
    private bool _hasClimbInput;
    private bool _hasJumpInput;
    private bool _hasJumpStarted;
    private bool _hasJumpEnded = true;
    private bool _hasSnapDisabled;
    private bool _canJump;
    private bool _canJumpFlag;
    private bool _hasInputsLocked;
    private bool _hasRecoiled;

    public override void _Ready()
    {
        base._Ready();
        AnimSprite = GetNode<AnimatedSprite>("AnimatedSprite");
        _jumpTimer = GetNode<Timer>("JumpTimer");
        _jumpTimer.Connect("timeout", this, nameof(OnJumpEnd));
    }

    public override void _Process(float delta)
    {
        if (_hasInputsLocked) return;
        base._Process(delta);
        AxisInputs();
        JumpInput();
        ClimbInput();
        DropFromPlatformInput();
        AnimationController();
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        CheckGround();
        CheckPlatform();
        CheckEdge();
        CheckCanJump();
        Velocity = CalculateMotionVelocity(delta) + CalculateSnapVelocity();
        Velocity = MoveAndSlideInArea(Velocity, delta, Vector2.Up);
    }
    
    protected virtual void OnDie()
    {
        LockInputs(true);
    }
    
    protected void LockInputs(bool value)
    {
        _hasInputsLocked = value;
        _inputAxis = Vector2.Zero;
        _desiredMove = 0;
        _desiredJumpSpeedX = 0;
    }
    
    protected void SetRecoil(bool value, Vector2? hitNormal)
    {
        _hasRecoiled = value;
        if (!_hasRecoiled) return;
        AnimSprite.Play("idle");
        _recoilHitNormal = hitNormal ?? _recoilHitNormal;
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
            _hasDropFromPlatformInput = true;
    }

    private void ClimbInput()
    {
        if (_isHangingOnEdge && Input.IsActionJustPressed("move_up"))
            _hasClimbInput = true;
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
            HasGroundRayDisabled = true;
            _hasJumpStarted = true;
            _hasJumpInput = true;
            _jumpTimer.Start(JumpDur);
        }

        if (Input.IsActionJustReleased("jump"))
        {
            OnJumpEnd();
        }
    }

    private void OnJumpEnd()
    {
        if (_hasJumpEnded) return;
        _hasJumpInput = false;
        _hasJumpEnded = true;
        _jumpTimer.Stop();
    }

    private Vector2 CalculateSnapVelocity()
    {
        if (_hasSnapDisabled) return Vector2.Zero;

        // Snap while the player moves on the ground.
        if (IsOnGround)
        {
            float dif = GroundHitPos.y - GlobalPosition.y;
            if (Mathf.Abs(dif) > 0.001f)
                return Mathf.Sign(dif) * Vector2.Down * _snapSpeed;
            return Vector2.Zero;
        }
        
        // Snap while the player hangs on the edge.
        if (_isHangingOnEdge)
        {
            Vector2 difVec = new Vector2(
                _edgeHitPos.x - (GlobalPosition.x + Direction * ShapeExtents.x),
                _edgeHitPos.y - (GlobalPosition.y - ShapeSizes.y)
            );
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
            if (_hasClimbInput)
            {
                velocity.y = -ClimbSpeedY;
                _hasSnapDisabled = true;
                return velocity;
            }
            velocity.y = 0f;
            return velocity;
        }

        // First frame after the edge is crossed.
        if (_hasClimbInput)
        {
            _hasClimbInput = false;
            _hasSnapDisabled = false;
            velocity.x = Direction * ClimbSpeedX;
            return velocity;
        }

        // While the player is on the ground.
        if (IsOnGround) 
        {
            // First frame when the player is on the ground.
            OnJumpEnd();
            
            // First frame when the player starts jumping.
            if (_canJump && _hasJumpStarted)
            {
                _canJump = false;
                _hasJumpEnded = false;
                IsOnGround = false;
                velocity.x = _desiredJumpSpeedX;
                velocity.y = -JumpInitialSpeedY;
                return velocity;
            }
            
            // First frame when the player starts dropping from a platform.
            if (_hasDropFromPlatformInput)
            {
                IsOnGround = false;
                SetCollisionMaskBit(2, false); // Layer 3 = false
                GroundCollisionMask -= (uint)Mathf.Pow(2, 3-1); // Layer 3 = false;
                return velocity;
            }

            // First frame when the player recoiled. 
            if (_hasRecoiled)
            {
                _hasRecoiled = false;
                velocity.x = Mathf.Sign(_recoilHitNormal.x) * RecoilSpeed;
                velocity.y = 0f;
                return velocity;
            }
            
            // While the player is walking on the ground.
            velocity.x = Mathf.MoveToward(velocity.x, _desiredMove, _moveAcceleration * delta);
            velocity.y = 0f; 
            return velocity;
        }
        
        // While the player is in the air.
        
        // First frame when the player recoiled. 
        if (_hasRecoiled)
        {
            _hasRecoiled = false;
            velocity.x = Mathf.Sign(_recoilHitNormal.x) * RecoilSpeed;
            velocity.y = 0;
            return velocity;
        }
        
        if (_canJump)
        {
            // First frame when the player starts jumping.
            if (_hasJumpStarted)
            {
                _canJump = false;
                _hasJumpEnded = false;
                IsOnGround = false;
                velocity.x = _desiredJumpSpeedX;
                velocity.y = -JumpInitialSpeedY;
                return velocity;
            }
        }
        else
        {
            // Second frame when the player starts jumping.
            if (_hasJumpStarted)
            {
                _hasJumpStarted = false;
                HasGroundRayDisabled = false;
            }
        }
       
        // While the player keep pressing the jump button in the air.
        if (_hasJumpInput)
        {
            if (Velocity.y > 0f)
                OnJumpEnd();
            else
                velocity.y -= JumpAccelerationY * delta;
        }
        
        velocity.x = Mathf.MoveToward(velocity.x, _desiredJumpSpeedX, _jumpAccelerationX * delta);
        velocity.y += Gravity * delta; // Adds gravity force increasingly.
        return velocity;
    }

    private void CheckCanJump()
    {
        if (_isHangingOnEdge || !_hasJumpEnded)
        {
            _canJump = false;
            return;
        }
        if (IsOnGround)
        {
            _canJump = true;
            _canJumpFlag = true;
            return;
        }
        if (GroundRay.Count <= 0)
        {
            if (!_canJumpFlag) return;
            _canJumpFlag = false;
            CanJumpDuration();
        }
        else
            _canJump = true;
    }

    private async void CanJumpDuration()
    {
        await ToSignal(GetTree().CreateTimer(_canJumpDur), "timeout");
        _canJump = false;
    }

    private void CheckGround()
    {
        if (GroundRay.Count <= 0
            || !IsGroundAngleEnough(CalculateGroundAngle((Vector2) GroundRay["normal"]), 5f)
            || GroundHitPos.DistanceTo(GlobalPosition) >= _isOnGroundDetectionLength)
        {
            IsOnGround = false;
            return;
        }
        IsOnGround = true;
    }

    private void CheckPlatform()
    {
        if (!IsOnGround || !(GroundRay["collider"] is CollisionObject2D ground))
        {
            _isOnPlatform = false;
            return;
        }

        foreach (int id in ground.GetShapeOwners())
            _isOnPlatform = ground.IsShapeOwnerOneWayCollisionEnabled((uint)id);
    }

    private void CheckEdge()
    {
        if (IsOnGround) return;
        if (_hasJumpInput) return;

        float rayPosX = ShapeExtents.x + _edgeAxisXRayLength;
        float rayPosY = -ShapeSizes.y - _edgeAxisYRayLength;
        // Checks whether there are inner collisions.
        _edgeRay = SpaceState.IntersectRay(
            GlobalPosition + new Vector2(Direction * ShapeExtents.x, -ShapeSizes.y),
            GlobalPosition + new Vector2(Direction * ShapeExtents.x, -ShapeSizes.y + 2f),
            new Array {this},
            GroundCollisionMask
        );
        // If there is an inner collision, does not check for a wall.
        if (_edgeRay.Count > 0) return;
        
        // Checks whether there is a wall in front of the player.
        _edgeRay = SpaceState.IntersectRay(
            GlobalPosition + new Vector2(Direction * ShapeExtents.x, rayPosY),
            GlobalPosition + new Vector2(Direction * rayPosX, rayPosY),
            new Array {this},
            GroundCollisionMask
        );
        // If there is a wall in front of the player, does not check for an edge.
        if (_edgeRay.Count > 0) return;
    
        // Checks whether there is an edge.
        _edgeRay = SpaceState.IntersectRay(
            GlobalPosition + new Vector2(Direction * rayPosX, rayPosY),
            GlobalPosition + new Vector2(Direction * rayPosX, -ShapeSizes.y),
            new Array {this},
            GroundCollisionMask
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
        _edgeRay = SpaceState.IntersectRay(
            GlobalPosition + new Vector2(Direction * ShapeExtents.x, 0f),
            GlobalPosition + new Vector2(Direction * rayPosX, 0f),
            new Array {this},
            GroundCollisionMask
        );
        _isHangingOnEdge = _edgeRay.Count > 0;
    }
    
    private void AnimationController()
    {
        if (_inputAxis.x > 0)
            Direction = 1;
        else if (_inputAxis.x < 0)
            Direction = -1;
        
        switch (Direction)
        {
            case 1:
                AnimSprite.FlipH = false;
                break;
            case -1:
                AnimSprite.FlipH = true;
                break;
        }
        
        if (IsOnGround)
            AnimSprite.Play(Velocity.x == 0 ? "idle" : "run");
        else
            AnimSprite.Play("jump");
    }

    private void OnPlatformBodyEntered(Node body) => HasGroundRayDisabled = true;
    
    private void OnPlatformBodyExited(Node body)
    {
        HasGroundRayDisabled = false;
        if (!_hasDropFromPlatformInput) return;
        
        _hasDropFromPlatformInput = false;
        SetCollisionMaskBit(2, true); // Layer 3 = true
        GroundCollisionMask += (uint)Mathf.Pow(2, 3-1); // Layer 3 = true
    }

    // public override void _Draw()
    // {
    //     float rayPosX = ShapeExtents.x + _edgeAxisXRayLength;
    //     float rayPosY = -ShapeSizes.y - _edgeAxisYRayLength;
    //     
    //     DrawLine(Vector2.Zero,
    //              Vector2.Down * GroundRayLength,
    //              IsOnGround ? Colors.Green : Colors.Red
    //     );
    //     DrawLine(new Vector2(Direction * ShapeExtents.x, rayPosY),
    //              new Vector2(Direction * rayPosX, rayPosY),
    //              Colors.Red
    //     );
    //     DrawLine(new Vector2(Direction * rayPosX, rayPosY),
    //              new Vector2(Direction * rayPosX, -ShapeSizes.y),
    //              _isHangingOnEdge ? Colors.Green : Colors.Red
    //     );
    // }
}
