using Game.Interface;
using Game.Service;
using Game.Service.Debug;

namespace Game.Scene
{
    public class MenuScene : SceneBase
    {
        public override SceneBase Init(App app, Load load, TreeTimer treeTimer, DebugOverlay debugOverlay)
        {
            base.Init(app, load, treeTimer, debugOverlay);
            Interface.GetNode<MainMenuPanel>("MainMenuPanel").Init();
            return this;
        }
    }
}
