using System.Collections.Generic;
using Godot;
using Other;

namespace Manager
{
    public class TreeTimer : Singleton<TreeTimer>
    {
        private Queue<TimerX> _timerPool;

        public override void _EnterTree()
        {
            SetSingleton();
            _timerPool = new Queue<TimerX>();
        }

        public SignalAwaiter Wait(float duration)
        {
            TimerX timer = Pull();
            timer.WaitTime = duration;
            timer.Start();
            return ToSignal(timer, "timeout");
        }
        
        public void Push(TimerX timer) => _timerPool.Enqueue(timer);

        private TimerX Pull()
        {
            if (_timerPool.Count <= 0) ExpandTimerPool();
            return _timerPool.Dequeue();
        }

        private void ExpandTimerPool()
        {
            TimerX timer = new TimerX();
            timer.Connect("timeout", timer, "PushToPool");
            AddChild(timer);
            _timerPool.Enqueue(timer);
        }
    }
}
