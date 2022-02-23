using Godot;
using AI;
using Godot.Collections;
using Manager;
using NavTool;
using Other;

namespace PlayerStateMachine
{
    public class Player : NavBody2D
    {
        [Export]
        public bool DebugEnabled { get; private set; }
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity { get; private set; } = 1100f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        public float GravitySpeedMax { get; private set; } = 225f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        public float AirAccelerationX { get; private set; } = 300f;
        [Export(PropertyHint.Layers2dPhysics)]
        public uint PlatformLayer { get; private set; } = 4;
        [Export]
        public Color SpriteColor { get; private set; }
        [Export]
        public MoveState MoveState { get; private set; }
        [Export]
        public FallState FallState { get; private set; }
        [Export]
        public JumpState JumpState { get; private set; }
        [Export]
        public RecoilState RecoilState { get; private set; }
        [Export]
        public DeadState DeadState { get; private set; }
        [Export]
        public PlatformState PlatformState { get; private set; }
        [Export]
        public WallState WallState { get; private set; }
        [Export]
        public WallJumpState WallJumpState { get; private set; }
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _maxHealth = 6;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float _wallRayLength = 2f;
        
        
        public Sprite Sprite { get; private set; }
        public AnimationPlayer AnimPlayer { get; private set; }
        public Area2D PlatformCheckArea { get; private set; }

        public enum PlayerStates { Move, Fall, Jump, Recoil, Dead, Platform, Wall, WallJump }
        public StateMachine<PlayerStates> Fsm { get; private set; }
        
        public Vector2 SnapVector => SnapDisabled ? Vector2.Zero : Vector2.Down * 2f;
        public Vector2 WallDirection { get; private set; }
        public bool SnapDisabled { get; set; }
        public bool IsCollidingWithPlatform { get; private set; }
        public bool FallOffPlatformInput;
        public bool IsWallRayHit { get; private set; }
        public new bool IsOnWall =>
            IsWallRayHit && IsOnWall() && Mathf.Sign(WallDirection.x) == Mathf.Sign(AxisInputs().x);
        private Vector2 _inputAxis;
        private int CoinCount { get; set; } = 0;
        private int _health;
        private int Health
        {
            get => _health;
            set
            {
                if (value < 0)
                    _health = 0;
                else if (value > _maxHealth)
                    _health = _maxHealth;
                else
                    _health = value;
            }
        }
        
        public override void _EnterTree()
        {
            base._EnterTree();
            AddToGroup("Player");
        }

        public override void _Ready()
        {
            base._Ready();
            Sprite = GetNode<Sprite>("Sprite");
            AnimPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            PlatformCheckArea = GetNode<Area2D>("PlatformCheckArea");
            Health = _maxHealth;
            Sprite.SelfModulate = SpriteColor;
            Fsm = new StateMachine<PlayerStates>();
            MoveState.Initialize(this);
            FallState.Initialize(this);
            JumpState.Initialize(this);
            RecoilState.Initialize(this);
            DeadState.Initialize(this);
            PlatformState.Initialize(this);
            WallState.Initialize(this);
            WallJumpState.Initialize(this);
            PlatformCheckArea.Connect("body_entered", this, "OnPlatformEntered");
            PlatformCheckArea.Connect("body_exited", this, "OnPlatformExited");
            PlatformCheckArea.Connect("body_exited", MoveState, "OnPlatformExited");
            Events.S.Connect("Damaged", this, nameof(OnDamaged));
            Events.S.Connect("CoinCollected", this, nameof(AddCoin));
            Fsm.SetCurrentState(PlayerStates.Fall);
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            Fsm._Process(delta);
            DirectionControl();
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            Fsm._PhysicsProcess(delta);
        }

        public void OnDamaged(
            NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (IsDead || target != this || IsUnhurtable) return;
            Health -= damageValue;
            Events.S.EmitSignal("PlayerHealthChanged", Health, _maxHealth, attacker);
            if (Health == 0)
            {
                IsDead = true;
                RecoilState.HitNormal = hitNormal;
                Fsm.SetCurrentState(PlayerStates.Recoil);
                Events.S.EmitSignal("PlayerDied");
            }
            else
            {
                RecoilState.HitNormal = hitNormal;
                Fsm.SetCurrentState(PlayerStates.Recoil);
            }
        }
        
        public Vector2 AxisInputs()
        {
            _inputAxis = new Vector2
            {
                x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
                y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up")
            };
            return _inputAxis = new Vector2(Mathf.Sign(_inputAxis.x), Mathf.Sign(_inputAxis.y));
        }

        public void CastWallRay()
        {
            var wallRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ExtentsHalf.x + 2f, -ExtentsHalf.y),
                GlobalPosition + new Vector2(-ExtentsHalf.x - _wallRayLength, -ExtentsHalf.y),
                new Array {this}, GroundLayer
            );
            if (wallRay.Count > 0)
            {
                IsWallRayHit = true;
                Vector2 hitPos = (Vector2)wallRay["position"];
                WallDirection = new Vector2(hitPos.x - GlobalPosition.x, 0).Normalized();
                return;
            }
            
            wallRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ExtentsHalf.x - 2f, -ExtentsHalf.y),
                GlobalPosition + new Vector2(ExtentsHalf.x + _wallRayLength, -ExtentsHalf.y),
                new Array {this}, GroundLayer
            );
            if (wallRay.Count > 0)
            {
                IsWallRayHit = true;
                Vector2 hitPos = (Vector2)wallRay["position"];
                WallDirection = new Vector2(hitPos.x - GlobalPosition.x, 0).Normalized();
                return;
            }
            
            IsWallRayHit = false;
        }
        
        private void OnPlatformEntered(Node body) => IsCollidingWithPlatform = true;

        private void OnPlatformExited(Node body) => IsCollidingWithPlatform = false;

        private void AddCoin(Node collector, Coin coin)
        {
            if (collector != this) return;
            CoinCount += coin.Value;
            Events.S.EmitSignal("PlayerCoinCountChanged", CoinCount);
        }
        
        private void DirectionControl()
        {
            if (_inputAxis.x > 0) Direction.x = 1;
            else if (_inputAxis.x < 0) Direction.x = -1;
            switch (Direction.x)
            {
                case 1:
                    Sprite.FlipH = false;
                    break;
                case -1:
                    Sprite.FlipH = true;
                    break;
            }
        }
    }
}

