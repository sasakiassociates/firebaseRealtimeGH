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
            // TODO add some runtime messages to see how many times this has been called
            // Add timestamp when it is called and when it is finished
            // There is a queue thing happening here where the actions are waiting for the lock to be released and only passing when it gets lucky
            // TODO add log here for showing it is still locked
            // Add a counter? to organize the queue of actions that are waiting for the lock to be released

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