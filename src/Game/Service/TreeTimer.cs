using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Game.Abstraction;

namespace Game.Service
{
    public class TreeTimer : Node, ISingleton
    {
        public static TreeTimer Singleton { get; private set; }
        private Queue<Timer> _timerPool;

        public TreeTimer Init()
        {
            Singleton = this;
            _timerPool = new Queue<Timer>();
            return this;
        }

        public SignalAwaiter Wait(float duration, PauseModeEnum pauseModeEnum = PauseModeEnum.Stop)
        {
            Timer timer = Pull();
            timer.PauseMode = pauseModeEnum;
            timer.WaitTime = duration;
            timer.Start();
            return ToSignal(timer, "timeout");
        }
        
        private void Push(Timer timer) => _timerPool.Enqueue(timer);

        private Timer Pull()
        {
            if (_timerPool.Count <= 0) ExpandTimerPool();
            return _timerPool.Dequeue();
        }

        private void ExpandTimerPool()
        {
            Timer timer = new Timer();
            timer.Connect("timeout", this, "OnTimerTimeout", new Array {timer});
            AddChild(timer, true);
            _timerPool.Enqueue(timer);
        }

        private void OnTimerTimeout(Timer timer)
        {
            timer.Stop();
            Push(timer);
        }
    }
}
