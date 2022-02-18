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
        public StateMachine<PlayerStates> Fsm { get; } = new StateMachine<PlayerStates>();
        public enum PlayerStates { Move, Fall, Jump, Recoil, Dead }
        
        public Sprite Sprite { get; private set; }
        public AnimationPlayer AnimPlayer { get; private set; }
        public Area2D PlatformCheckArea { get; private set; }
        public Timer JumpTimer { get; private set; }

        [Export]
        public bool DebugEnabled { get; private set; }
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity { get; private set; } = 1100f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        public float GravitySpeedMax { get; private set; } = 225f;
        [Export(PropertyHint.Range, "0.1,20,0.05,or_greater")] 
        protected float GroundRayLength { get; private set; } = 5f;
        [Export(PropertyHint.Layers2dPhysics)]
        public uint PlatformMask { get; private set; } = 4;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int MaxHealth { get; set; } = 6;
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
       
        public Dictionary GroundRay { get; private set; }
        public Vector2 GroundHitPos { get; private set; }
        private Vector2 _inputAxis;
        public int CoinCount { get; private set; } = 0;
        private int _health;
        public int Health
        {
            get => _health;
            private set
            {
                if (value < 0)
                    _health = 0;
                else if (value > MaxHealth)
                    _health = MaxHealth;
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
            JumpTimer = GetNode<Timer>("JumpTimer");
            MoveState.Initialize(this);
            FallState.Initialize(this);
            JumpState.Initialize(this);
            RecoilState.Initialize(this);
            DeadState.Initialize(this);
            PlatformCheckArea.Connect("body_exited", MoveState, "OnPlatformExited");
            JumpTimer.Connect("timeout", JumpState, "OnJumpEnd");
            Health = MaxHealth;
            Sprite.SelfModulate = SpriteColor;
            Events.Singleton.Connect("Damaged", this, nameof(OnDamaged));
            Events.Singleton.Connect("CoinCollected", this, nameof(AddCoin));
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
            if (CurNavBodyType == NavBodyType.Platformer) CastGroundRay();
            Fsm._PhysicsProcess(delta);
        }
        
        public void OnDamaged(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (IsDead || target != this || IsUnhurtable) return;
            
            Health -= damageValue;
            Events.Singleton.EmitSignal("PlayerHealthChanged", Health, MaxHealth, attacker);
            if (Health == 0)
            {
                IsDead = true;
                RecoilState.HitNormal = hitNormal;
                Fsm.SetCurrentState(PlayerStates.Recoil);
                Events.Singleton.EmitSignal("PlayerDied");
            }
            else
            {
                RecoilState.HitNormal = hitNormal;
                Fsm.SetCurrentState(PlayerStates.Recoil);
            }
        }
        
        private void AddCoin(Node collector, Coin coin)
        {
            if (collector != this) return;
            CoinCount += coin.Value;
            Events.Singleton.EmitSignal("PlayerCoinCountChanged", CoinCount);
        }

        public Vector2 AxisInputs()
        {
            _inputAxis.x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            _inputAxis.y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
            return _inputAxis;
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
        
        private void CastGroundRay()
        {
            // Raycast from the left bottom corner.
            GroundRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ExtentsHalf.x, -5f),
                GlobalPosition + new Vector2(-ExtentsHalf.x, GroundRayLength),
                new Array {this},
                GroundMask
            );
            if (GroundRay.Count > 0)
            {
                GroundHitPos = (Vector2) GroundRay["position"] + new Vector2(ExtentsHalf.x, 0);
                return;
            }

            // If the first raycast does not hit the ground.
            // Raycast from the right bottom corner.
            GroundRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ExtentsHalf.x, -5f),
                GlobalPosition + new Vector2(ExtentsHalf.x, GroundRayLength),
                new Array {this},
                GroundMask
            );
            if (GroundRay.Count > 0)
            {
                GroundHitPos = (Vector2) GroundRay["position"] + new Vector2(-ExtentsHalf.x, 0);
            }
        }
    }
}

