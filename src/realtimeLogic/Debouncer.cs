using System;
using System.Threading;

namespace realtimeLogic
{
    public class Debouncer
    {
        private Timer timer;
        public int update_interval = 33;

        public Debouncer() { }

        public void SetDebounceDelay(int milliseconds)
        {
            update_interval = milliseconds;
        }

        public void Debounce(Action action)
        {
            if (timer == null)
            {
                timer = new Timer(state =>
                {
                    timer?.Dispose();
                    timer = null;
                    action();
                    // TODO add action and timestamp here
                }, null, update_interval, Timeout.Infinite);
            }
        }
    }
}