using Godot;
using Godot.Collections;
using NavTool;
using Other;

namespace Manager
{
    public class Events : Singleton<Events>
    {
        [Signal]
        private delegate void SceneUnloaded();
        [Signal]
        private delegate void SceneLoaded();
        [Signal]
        private delegate void Damaged(
            NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal);
        [Signal]
        private delegate void CoinCollected(Node collector, Coin coin);
        [Signal] 
        private delegate void PlayerCoinCountChanged(int coinCount);
        [Signal]
        private delegate void PlayerHealthChanged(int newHealth, int maxHealth, NavBody2D attacker);
        [Signal] 
        private delegate void PlayerDied();

        public override void _EnterTree()
        {
            SetSingleton();
            Connect(nameof(SceneUnloaded), this, nameof(OnSceneUnloaded));
        }

        private void OnSceneUnloaded()
        {
            Array connectionList = GetSignalConnectionList(nameof(Damaged));
            foreach (object connection in connectionList)
            {
                GD.Print(connection.ToString());
            }
        }
    }
}
