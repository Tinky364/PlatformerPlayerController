using Game.Fsm;
using Game.Level.Players.States;
using Godot;
using Godot.Collections;
using NavTool;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.Players
{
    public class Player : NavBody2D
    {
        [Signal] 
        public delegate void CoinCountChanged(int coinCount);
        
        [Export]
        public bool DebugEnabled { get; private set; }
        [Export]
        public HealthSystem HealthSystem { get; private set; }
        [Export(PropertyHint.Range, "10,2000,or_greater")]
        public float Gravity { get; private set; } = 900f;
        [Export(PropertyHint.Range, "100,1000,or_greater,or_lesser")]
        public float GravitySpeedMax { get; private set; } = 150f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        public float AirAccelerationX { get; private set; } = 375f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float WallRayLength { get; set; } = 2f;
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private float WallRayOffsetY { get; set; } = 4f;
        [Export(PropertyHint.Layers2dPhysics)]
        public uint PlatformLayer { get; private set; } = 4;
        [Export]
        public Color NormalSpriteColor { get; private set; }
        [Export]
        public PlayerStateMove PlayerStateMove { get; private set; }
        [Export]
        public PlayerStateFall PlayerStateFall { get; private set; }
        [Export]
        public PlayerStateJump PlayerStateJump { get; private set; }
        [Export]
        public PlayerStateWalljump PlayerStateWalljump { get; private set; }
        [Export]
        public PlayerStateDash PlayerStateDash { get; private set; }
        [Export]
        public PlayerStateRecoil PlayerStateRecoil { get; private set; }
        [Export]
        public PlayerStateWall PlayerStateWall { get; private set; }
        [Export]
        public PlayerStatePlatform PlayerStatePlatform { get; private set; }
        [Export]
        public PlayerStateDead PlayerStateDead { get; private set; }
       
        public enum PlayerStates { Move, Fall, Jump, Recoil, Dead, Platform, Wall, Walljump, Dash }
        public FiniteStateMachine<Player, PlayerStates> Fsm { get; private set; }
        public Sprite Sprite { get; private set; }
        public AnimationPlayer AnimPlayer { get; private set; }
        public Area2D PlatformCheckArea { get; private set; }
        public CollisionShape2D CollisionShape { get; private set; }
        public Vector2 PreVelocity { get; private set; }
        public Vector2 SnapVector => SnapDisabled ? Vector2.Zero : Vector2.Down * 2f;
        public Vector2 WallDirection { get; private set; }
        public Vector2 MoveDirectionAvg { get; private set; }
        public bool SnapDisabled { get; set; }
        public bool IsCollidingWithPlatform { get; private set; }
        public bool FallOffPlatformInput;
        public bool IsWallJumpAble { get; private set; }
        public bool IsWallRayHit { get; private set; }
        public bool IsStayOnWall => IsWallRayHit && IsOnWall() && Mathf.Sign(WallDirection.x) == Mathf.Sign(AxisInputs().x);
        public bool IsDirectionLocked { get; set; }
        private readonly Vector2[] _dirs = new Vector2[10]; 
        private Vector2 PrePosition { get; set; }
        private Vector2 _inputAxis;
        private int CoinCount { get; set; } = 0;
        
        public Player Init()
        {
            AddToGroup("Player");
            Sprite = GetNode<Sprite>("Sprite");
            AnimPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            PlatformCheckArea = GetNode<Area2D>("PlatformCheckArea");
            CollisionShape = GetNode<CollisionShape2D>("CollisionShapeCapsule");
            Sprite.SelfModulate = NormalSpriteColor;
            HealthSystem.Init();
            Fsm = new FiniteStateMachine<Player, PlayerStates>();
            PlayerStateMove.Init(this);
            PlayerStateFall.Init(this);
            PlayerStateJump.Init(this);
            PlayerStateRecoil.Init(this);
            PlayerStateDead.Init(this);
            PlayerStatePlatform.Init(this);
            PlayerStateWall.Init(this);
            PlayerStateWalljump.Init(this);
            PlayerStateDash.Init(this);
            PlatformCheckArea.Connect("body_entered", this, "OnPlatformEntered");
            PlatformCheckArea.Connect("body_exited", this, "OnPlatformExited");
            Events.Singleton.Connect(nameof(Events.Damaged), this, nameof(OnDamaged));
            Events.Singleton.Connect(nameof(Events.CoinCollected), this, nameof(AddCoin));
            Fsm.ChangeState(PlayerStates.Fall);
            CalculateMoveDirectionAverage();
            return this;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            foreach (var pair in Fsm.States) pair.Value.ExitTree();
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            Fsm.Process(delta);
            DirectionControl();
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            Fsm.PhysicsProcess(delta);
            if (PreVelocity != Velocity) PreVelocity = Velocity;
        }

        public void OnDamaged(NavBody2D target, int damageAmount, NavBody2D attacker, Vector2 hitNormal)
        {
            if (HealthSystem.IsDied || IsUnhurtable || target != this) return;
            HealthSystem.Damage(damageAmount);
            PlayerStateRecoil.HitNormal = hitNormal;
            Fsm.ChangeState(PlayerStates.Recoil);
        }

        public Vector2 AxisInputs()
        {
            _inputAxis = new Vector2
            {
                x = InputInvoker.GetStrength("move_right") - InputInvoker.GetStrength("move_left"),
                y = InputInvoker.GetStrength("move_down") - InputInvoker.GetStrength("move_up")
            };
            return _inputAxis = new Vector2(Mathf.Sign(_inputAxis.x), Mathf.Sign(_inputAxis.y));
        }
        
        public void PlayAnimation(string name, float? duration = null)
        {
            float speed = 1f;
            if (duration != null) speed = AnimPlayer.GetAnimation(name).Length / duration.Value;
            AnimPlayer.PlaybackSpeed = speed;
            AnimPlayer.Play(name);
        }

        /// <summary>
        /// Detects wall when both up and down ray hits. It is enough to hit the down ray to execute
        /// wall jump. 
        /// </summary>
        public void CastWallRay()
        {
            // Left down ray
            var wallRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(-ExtentsHalf.x + 2f, -WallRayOffsetY),
                GlobalPosition + new Vector2(-ExtentsHalf.x - WallRayLength, -WallRayOffsetY),
                new Array {this}, GroundLayer
            );
            if (wallRay.Count > 0)
            {
                IsWallJumpAble = true;

                // Left up ray
                wallRay = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(-ExtentsHalf.x + 2f, -Extents.y + WallRayOffsetY),
                    GlobalPosition + new Vector2(-ExtentsHalf.x - WallRayLength, -Extents.y + WallRayOffsetY),
                    new Array {this}, GroundLayer
                );
                if (wallRay.Count > 0)
                {
                    IsWallRayHit = true;
                    Vector2 hitPos = (Vector2)wallRay["position"];
                    WallDirection = new Vector2(hitPos.x - GlobalPosition.x, 0f).Normalized();
                    return;
                }
                
                IsWallRayHit = false;
                return;
            }
            
            // Right down ray
            wallRay = SpaceState.IntersectRay(
                GlobalPosition + new Vector2(ExtentsHalf.x - 2f, -WallRayOffsetY),
                GlobalPosition + new Vector2(ExtentsHalf.x + WallRayLength, -WallRayOffsetY),
                new Array {this}, GroundLayer
            );
            if (wallRay.Count > 0)
            {
                IsWallJumpAble = true;

                // Right up ray
                wallRay = SpaceState.IntersectRay(
                    GlobalPosition + new Vector2(ExtentsHalf.x - 2f, -Extents.y + WallRayOffsetY),
                    GlobalPosition + new Vector2(ExtentsHalf.x + WallRayLength, -Extents.y + WallRayOffsetY),
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
                return;
            }
           
            IsWallJumpAble = false;
            IsWallRayHit = false;
        }
        
        private async void CalculateMoveDirectionAverage()
        {
            int index = 0;
            while (IsInstanceValid(this))
            {
                _dirs.SetValue(PrePosition.DirectionTo(GlobalPosition), index);
                PrePosition = GlobalPosition;
                if (index == 9) index = 0;
                else index++;
                if (index == 0)
                {
                    Vector2 total = Vector2.Zero;
                    for (int i = 0; i < _dirs.Length; i++)
                    {
                        total += _dirs[i];
                        if (i == _dirs.Length - 1) total += Direction;
                        MoveDirectionAvg = (total / 10f).Normalized();
                    }
                }
                await TreeTimer.Singleton.Wait(0.1f);
            }
        }

        private void OnPlatformEntered(Node body)
        {
            if (Velocity.y >= -90f) return;
            IsCollidingWithPlatform = true;
        }

        private void OnPlatformExited(Node body) => IsCollidingWithPlatform = false;

        private void AddCoin(Node collector, Coin coin)
        {
            if (collector != this) return;
            CoinCount += coin.Value;
            EmitSignal(nameof(CoinCountChanged), CoinCount);
        }
        
        private void DirectionControl()
        {
            if (IsDirectionLocked) return;
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

