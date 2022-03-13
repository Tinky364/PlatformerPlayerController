using Godot;

namespace AI.States
{
    public abstract class EnemyState : State<Enemy, Enemy.EnemyStates>
    {
        [Export]
        public Enemy.EnemyStates State { get; private set; }
    }
}