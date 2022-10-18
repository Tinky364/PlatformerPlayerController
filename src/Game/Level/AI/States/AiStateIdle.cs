using CustomRegister;
using Godot;
using Game.Fsm;
using Game.Service.Debug;

namespace Game.Level.AI.States
{
    [Register]
    public class AiStateIdle : State<Enemy, Enemy.EnemyStates>
    {
        [Export]
        private IdleType _idleType = IdleType.Stay;
        [Export]
        private float _secondPosDist = 40f;
        [Export]
        private float _stayDur = 2f;

        private enum IdleType { Stay, Move }
        private Vector2 _pos1 = new Vector2();
        private Vector2 _pos2 = new Vector2();
        private Vector2 _targetPos = new Vector2();
        private float _stayCount = 0;

        public override void Init(Enemy owner)
        {
            base.Init(owner);
            
            _pos1 = Owner.Agent.NavPos;
            switch (_idleType)
            {
                case IdleType.Stay:
                    _targetPos = _pos1;
                    break;
                case IdleType.Move: 
                    SetMovePosition();
                    _targetPos = _pos2;
                    break;
            }
        }
        
        public override void Enter()
        {
            Log.Info($"{Owner.Name}: {Key}");
        }

        public override void PhysicsProcess(float delta)
        {
            switch (_idleType)
            {
                case IdleType.Stay: StayLoop(delta);
                    break;
                case IdleType.Move: MoveLoop(delta);
                    break;
            }
        }

        public override void Process(float delta) { }
        public override void Exit() { }
        public override void ExitTree() { }
        public override bool CanChange() { return false; }

        private void SetMovePosition()
        {
            _pos2 = _pos1 + new Vector2(_secondPosDist, 0f);
            if (!Owner.Agent.NavArea.IsPositionInArea(_pos2))
                GD.PrintErr("Not enough space for the enemy idle motion!");
        }

        private void MoveLoop(float delta)
        {
            Vector2 distToTarget = _targetPos - Owner.Agent.NavPos;
            if (Mathf.Abs(distToTarget.x) > 1f) // Moves to the target position.
            {
                Owner.PlayAnimation("run");
                Owner.Agent.Direction.x = distToTarget.Normalized().x; // Direction to target.
                Owner.Agent.Velocity.x = Mathf.MoveToward(
                    Owner.Agent.Velocity.x, Owner.Agent.Direction.x * Owner.MoveSpeed, 
                    Owner.MoveAcceleration * delta
                );
            }
            else // When it is in the target position.
            {
                Owner.Agent.Velocity.x = 0;
                if (_stayCount < _stayDur) // Stays in idle position until duration expired.
                {
                    Owner.PlayAnimation("idle");
                    _stayCount += delta;
                }
                else // New target position is declared. 
                {
                    _targetPos = _targetPos == _pos2 ? _pos1 : _pos2;
                    _stayCount = 0;
                }
            }
        }

        private void StayLoop(float delta)
        {
            Vector2 distToTarget = _targetPos - Owner.Agent.NavPos;
            if (Mathf.Abs(distToTarget.x) > 1f) // Moves to the target position.
            {
                Owner.PlayAnimation("run");
                Owner.Agent.Direction.x = distToTarget.Normalized().x; // Direction to target.
                Owner.Agent.Velocity.x = Mathf.MoveToward(
                    Owner.Agent.Velocity.x, Owner.Agent.Direction.x * Owner.MoveSpeed, 
                    Owner.MoveAcceleration * delta
                );
            }
            else
            {
                Owner.Agent.Velocity.x = 0;
                Owner.PlayAnimation("idle");
            }
        }
    }
}