using Godot;
using AI;
using Other;

namespace Manager
{
    public class GameSceneSM : SceneManager
    {
        public override void _Ready()
        {
            base._Ready();

            if (!GetTree().HasGroup("Player")) return;
            if (GetTree().GetNodesInGroup("Player")?[0] is Other.Player player)
            {
                foreach (Enemy enemy in GetTree().GetNodesInGroup("Enemy"))
                {
                    if (enemy.Body.TargetNavBody != null) break;
                    enemy.Body.TargetNavBody = player;
                }
            }
        }
    }
}
