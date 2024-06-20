using System;
using System.Threading;

namespace realtimeLogic
{
    public class Debouncer
    {
        private readonly object lockObject = new object();
        private Timer timer;
        public int update_interval = 33;

        public Debouncer() { }

        public void SetDebounceDelay(int milliseconds)
        {
            update_interval = milliseconds;
        }

        public void Debounce(Action action)
        {
            lock (lockObject)
            {
                if (timer != null)
                {
                    timer.Change(update_interval, Timeout.Infinite);
                }
                else
                {
                    timer = new Timer(state =>
                    {
                        lock (lockObject)
                        {
                            timer?.Dispose();
                            timer = null;
                        }
                        action();
                    }, null, update_interval, Timeout.Infinite);
                }
            }
        }
    }
}