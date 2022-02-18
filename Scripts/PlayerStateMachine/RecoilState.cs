using Godot;
using System;
using System.Threading.Tasks;
using AI;

namespace PlayerStateMachine
{
    public class RecoilState : State<Player.PlayerStates>
    {
        private Player P { get; set; }
        
        [Export(PropertyHint.Range, "0,1000,or_greater")]
        private float _recoilImpulse = 150f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _unhurtableDur = 1f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _recoilDur = 0.5f;
        
        public Vector2? HitNormal;
        private float _count;

        public override async void Enter()
        {
            if (P.DebugEnabled) GD.Print($"{P.Name}: {Key}");
            
            _count = 0;
            P.AnimPlayer.Play("jump");

            P.Velocity = CalculateRecoilVelocity();
            
            P.IsUnhurtable = true;
            await WhileUnhurtable();
            if (P.IsDead) return;
            P.IsUnhurtable = false;
        }
        
        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            if (_count > _recoilDur)
            {
                if (P.IsDead)
                    P.Fsm.SetCurrentState(Player.PlayerStates.Dead);
                else if (P.IsOnFloor()) 
                    P.Fsm.SetCurrentState(Player.PlayerStates.Move);
                else 
                    P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }

            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, 0, 400 * delta);
            if (P.IsOnFloor()) P.Velocity.y = P.Gravity * delta;
            else
            { 
                if (P.Velocity.y < P.GravitySpeedMax) 
                    P.Velocity.y += P.Gravity * delta;
            }
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, Vector2.Down * 2f, Vector2.Up);
            
            _count += delta;
        }

        public override void Exit()
        {
            HitNormal = null;
        }

        private Vector2 CalculateRecoilVelocity()
        {
            Vector2 recoilDir = HitNormal ?? -P.Direction;
            Vector2 recoilVelocity = new Vector2
            {
                x = Mathf.Clamp(Mathf.Abs(recoilDir.x), 0.7f, 1f) * Mathf.Sign(recoilDir.x),
                y = Mathf.Clamp(Mathf.Abs(recoilDir.y), 0.2f, 1f) * Mathf.Sign(recoilDir.y)
            };
            recoilVelocity.x *= _recoilImpulse * 0.85f;
            recoilVelocity.y *= recoilDir.y < 0 ? _recoilImpulse * 1.5f : _recoilImpulse / 2f;
            return recoilVelocity;
        }
        
        private async Task WhileUnhurtable()
        {
            float count = 0f;
            while (count < _unhurtableDur)
            {
                P.Sprite.SelfModulate = 
                    P.Sprite.SelfModulate == Colors.White ? P.SpriteColor : Colors.White;
                float t = count / _unhurtableDur;
                t = 1 - Mathf.Pow(1 - t, 5);
                float waitTime = Mathf.Lerp(0.01f, 0.2f, t);
                count += waitTime;
                await ToSignal(P.GetTree().CreateTimer(waitTime), "timeout");
            }
            P.Sprite.SelfModulate = P.SpriteColor;
        }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Recoil);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}

