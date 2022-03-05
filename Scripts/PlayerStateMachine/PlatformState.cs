using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class PlatformState : State<Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _speedInsidePlatformY = 100f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationX = 300f;
        
        private Player P { get; set; }

        public void Initialize(Player player)
        {
            Initialize(Player.PlayerStates.Platform);
            P = player;
            P.Fsm.AddState(this);
        }
        
        public override void Enter()
        {
            GM.Print(P.DebugEnabled, $"{P.Name}: {Key}");
            P.SnapDisabled = true;
            P.PlayAnim("jump_side", 1f);
        }

        public override void PhysicsProcess(float delta)
        {
            P.Velocity = P.MoveAndSlideWithSnap(P.Velocity, P.SnapVector, Vector2.Up);

            if (!P.IsCollidingWithPlatform)
            {
                P.Velocity.y = 0;
                P.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
            P.Velocity.x = Mathf.MoveToward(P.Velocity.x, 0, _accelerationX * delta);
            P.Velocity.y = -_speedInsidePlatformY;
        }

        public override void Process(float delta) { }

        public override void Exit() { }
    }
}
