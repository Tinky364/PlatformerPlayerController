using Godot;
using Game.Level.AI.Enemies;
using Game.Interface;
using Game.Level;
using Game.Level.Players;
using Game.Service;
using Game.Service.Debug;

namespace Game.Scene
{
    public class BattleScene : SceneBase
    {
        private PausePanel _pausePanel;
        
        public override SceneBase Init(App app, Load load, TreeTimer treeTimer, DebugOverlay debugOverlay)
        {
            base.Init(app, load, treeTimer, debugOverlay);
            Player player = Level.GetNode<Player>("Player").Init();
            Level.GetNode<BigHammerEnemy>("BigHammerEnemy").Init();
            Level.GetNode<JumperEnemy>("JumperEnemy").Init();
            Level.GetNode<RusherEnemy>("RusherEnemy").Init();
            Level.GetNode<MovingPlatform>("MovingPlatform").Init();
            Level.GetNode<Door>("Door").Init();
            Level.GetNode<PixelCamera>("Camera").Init();
            Interface.GetNode<Hud>("Hud").Init(player);
            Interface.GetNode<GameOverPanel>("GameOverPanel").Init(player);
            _pausePanel = Interface.GetNode<PausePanel>("PausePanel").Init(player);
            GetTree().CallGroupFlags((int)SceneTree.GroupCallFlags.Realtime, "Enemy", "SetTarget", player);
            return this;
        }

        public override void _Input(InputEvent @event)
        {
            if (InputInvoker.IsPressed("ui_end"))
            {
                _pausePanel.PausePanelControl();
            }
        }
    }
}
