using CustomRegister;
using Godot;
using Game.Fsm;
using Game.Service.Debug;

namespace Game.Level.AI.States
{
    [Register]
    public class AiStateChase : State<Enemy, Enemy.EnemyStates>
    {
        [Export(PropertyHint.Range, "0,100,1,or_greater")]
        public float StopDist { get; private set; } = 26f;
        [Export(PropertyHint.Range, "1,100,1,or_greater")]
        public float StopDistThreshold { get; private set; } = 1f;
        [Export(PropertyHint.Range, "0,200,1,or_greater")]
        private float _chaseSpeed = 30f;

        public Vector2 TargetPos { get; set; }
        
        public override void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
            Owner.Agent.Velocity.x = 0f;
            Owner.PlayAnimation("run");
        }

        public override void PhysicsProcess(float delta)
        {
            Vector2 dirToTargetPos = Owner.Agent.NavPos.DirectionTo(TargetPos);
            Owner.Agent.Direction.x = dirToTargetPos.x;
            Owner.Agent.Velocity.x = Mathf.MoveToward(
                Owner.Agent.Velocity.x, Owner.Agent.Direction.x * _chaseSpeed,
                Owner.MoveAcceleration * delta);
        }
        
        public override void Process(float delta) { }
        public override void Exit() { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }
    }
}