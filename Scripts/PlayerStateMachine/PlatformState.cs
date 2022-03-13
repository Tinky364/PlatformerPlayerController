using AI;
using Godot;
using Manager;

namespace PlayerStateMachine
{
    public class PlatformState : State<Player, Player.PlayerStates>
    {
        [Export(PropertyHint.Range, "1,200,or_greater")]
        private float _speedInsidePlatformY = 100f;
        [Export(PropertyHint.Range, "1,2000,or_greater")]
        private float _accelerationX = 300f;
        
        public override void Initialize(Player owner, Player.PlayerStates key)
        {
            base.Initialize(owner, key);
            Owner.Fsm.AddState(this);
        }
        
        public override void Enter()
        {
            GM.Print(Owner.DebugEnabled, $"{Owner.Name}: {Key}");
            Owner.SnapDisabled = true;
            Owner.PlayAnimation("jump_side", 1f);
        }

        public override void PhysicsProcess(float delta)
        {
            Owner.Velocity = Owner.MoveAndSlideWithSnap(Owner.Velocity, Owner.SnapVector, Vector2.Up);

            if (!Owner.IsCollidingWithPlatform)
            {
                Owner.Velocity.y = 0;
                Owner.Fsm.SetCurrentState(Player.PlayerStates.Fall);
                return;
            }
            
            Owner.Velocity.x = Mathf.MoveToward(Owner.Velocity.x, 0, _accelerationX * delta);
            Owner.Velocity.y = -_speedInsidePlatformY;
        }

        public override void Process(float delta) { }

        public override void Exit() { }
        public override void ExitTree() { }
    }
}
