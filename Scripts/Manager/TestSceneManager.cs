using AI;
using PlayerStateMachine;

namespace Manager
{
    public class TestSceneManager : SceneManager
    {
        public override void _Ready()
        {
            base._Ready();

            if (!GetTree().HasGroup("Player")) return;
            if (GetTree().GetNodesInGroup("Player")[0] is Player player)
            {
                foreach (Enemy enemy in GetTree().GetNodesInGroup("Enemy"))
                {
                    if (enemy.Agent.TargetNavBody != null) break;
                    enemy.Agent.TargetNavBody = player;
                }
            }
        }
    }
}
