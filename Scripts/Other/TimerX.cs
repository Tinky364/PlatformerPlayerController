using Godot;
using Manager;

namespace Other
{
    public class TimerX : Timer
    {
        private void PushToPool()
        {
            Stop();
            TreeTimer.S.Push(this);
        }
    }
}

