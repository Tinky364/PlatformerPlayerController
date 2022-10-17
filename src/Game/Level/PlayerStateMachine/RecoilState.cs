using System.Threading.Tasks;
using CustomRegister;
using Godot;
using Game.Fsm;
using Game.Service;
using Game.Service.Debug;

namespace Game.Level.PlayerStateMachine
{
    [Register]
    public class RecoilState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "0,1000,or_greater")]
        private float _impulse = 150f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _recoilDur = 0.25f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _unhurtableDur = 1f;
        [Export]
        private Color _unhurtableSpriteColor;
        
        public Vector2? HitNormal { get; set; }
        private float _count;
        private Vector2 _desiredRecoilVelocity;

        public override async void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            _count = 0;
            Owner.SnapDisabled = false;
            Owner.PlayAnimation("jump_side", 1f);
            _desiredRecoilVelocity = CalculateRecoilImpulse();
            Owner.Velocity = _desiredRecoilVelocity;
            Owner.IsUnhurtable = true;
            await WhileUnhurtable();
            if (Owner.IsDead) return;
            Owner.IsUnhurtable = false;
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            if (_count > _recoilDur)
            {
                if (Owner.IsDead) Owner.Fsm.ChangeState(Player.PlayerStates.Dead);
                else if (Owner.IsOnFloor()) Owner.Fsm.ChangeState(Player.PlayerStates.Move);
                else Owner.Fsm.ChangeState(Player.PlayerStates.Fall);
                return;
            }
            _count += delta;
            
            Owner.Velocity.x = Mathf.MoveToward(
                Owner.Velocity.x, 0, Mathf.Abs(_desiredRecoilVelocity.x / _recoilDur) * delta
            );
            if (Owner.IsOnFloor()) Owner.Velocity.y = Owner.Gravity * delta;
            else if (Owner.Velocity.y < Owner.GravitySpeedMax) Owner.Velocity.y += Owner.Gravity * delta;
        }

        public override void Exit()
        {
            HitNormal = null;
        }
        
        public override void Process(float delta) { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }

        private Vector2 CalculateRecoilImpulse()
        {
            Vector2 recoilDir = HitNormal ?? -Owner.Direction;
            Vector2 recoilVelocity = new Vector2
            {
                x = Mathf.Clamp(Mathf.Abs(recoilDir.x), 0.7f, 1f) * Mathf.Sign(recoilDir.x),
                y = Mathf.Clamp(Mathf.Abs(recoilDir.y), 0.2f, 1f) * Mathf.Sign(recoilDir.y)
            };
            recoilVelocity.x *= _impulse * 0.85f;
            recoilVelocity.y *= recoilDir.y < 0 ? _impulse * 1.5f : _impulse / 2f;
            return recoilVelocity;
        }
        
        private async Task WhileUnhurtable()
        {
            float count = 0f;
            while (count < _unhurtableDur)
            {
                if (!IsInstanceValid(Owner)) return;
                Owner.Sprite.SelfModulate = Owner.Sprite.SelfModulate == _unhurtableSpriteColor
                    ? Owner.NormalSpriteColor
                    : _unhurtableSpriteColor;
                float t = count / _unhurtableDur;
                t = 1 - Mathf.Pow(1 - t, 5);
                float waitTime = Mathf.Lerp(0.01f, 0.2f, t);
                count += waitTime;
                await TreeTimer.Singleton.Wait(waitTime);
            }
            Owner.Sprite.SelfModulate = Owner.NormalSpriteColor;
        }
    }
}

