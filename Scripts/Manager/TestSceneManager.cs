using Godot;
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
                GetTree().CallGroupFlags(
                    (int) SceneTree.GroupCallFlags.Realtime,
                    "Enemy",
                    "SetTarget",
                    player
                );
            }
        }
    }
}
