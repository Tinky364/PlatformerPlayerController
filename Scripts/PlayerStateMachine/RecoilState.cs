using Godot;
using System.Threading.Tasks;
using AI;
using Manager;

namespace PlayerStateMachine
{
    public class RecoilState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "0,1000,or_greater")]
        private float _impulse = 150f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _recoilDur = 0.5f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _unhurtableDur = 1f;
        
        private Player P { get; set; }

        public Vector2? HitNormal { get; set; }
        private float _count;

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Recoil);
            P = player;
            P.Fsm.AddState(this);
        }
        
        public override async void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            _count = 0;
            P.SnapDisabled = false;
            P.AnimPlayer.Play("jump");
            P.Velocity = CalculateRecoilImpulse();
            P.IsUnhurtable = true;
            await WhileUnhurtable();
            if (P.IsDead) return;
            P.IsUnhurtable = false;
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (_count > _recoilDur)
            {
                if (P.IsDead) P.Fsm.SetCurrentState(Player.PlayerStates.Dead);
                else if (P.IsOnFloor()) P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                else P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }

            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, 0, _recoilDur / delta);
            if (P.IsOnFloor()) P.Velocity.y = P.Gravity * delta;
            else if (P.Velocity.y < P.GravitySpeedMax) P.Velocity.y += P.Gravity * delta;
            
            _count += delta;
        }

        public override void Exit()
        {
            HitNormal = null;
        }

        public override void Process(float delta) { }
        
        private Vector2 CalculateRecoilImpulse()
        {
            Vector2 recoilDir = HitNormal ?? -P.Direction;
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
                if (!IsInstanceValid(P)) return;
                P.Sprite.SelfModulate = P.Sprite.SelfModulate == Colors.White
                    ? P.SpriteColor
                    : Colors.White;
                float t = count / _unhurtableDur;
                t = 1 - Mathf.Pow(1 - t, 5);
                float waitTime = Mathf.Lerp(0.01f, 0.2f, t);
                count += waitTime;
                await TreeTimer.S.Wait(waitTime);
            }
            P.Sprite.SelfModulate = P.SpriteColor;
        }
    }
}

