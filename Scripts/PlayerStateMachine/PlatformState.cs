using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class PlatformState : State<Player.PlayerStates>
    {
        private Player P { get; set; }
        
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _speedY = 70f;

        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.AnimPlayer.Play("jump");
        }

        public override void Process(float delta) { }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (!P.IsCollidingWithPlatform)
            {
                P.Velocity.y = 0;
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
            P.Velocity.y = -_speedY;
        }

        public override void Exit() { }
        
        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Platform);
            P = player;
            P.Fsm.AddState(this);
        }
    }
}
