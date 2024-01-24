using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace realtimeLogic
{
    internal class Debouncer
    {
        private Debouncer() { }
        private static Debouncer instance;
        public static Debouncer GetInstance()
        {
            if (instance == null)
            {
                lock (typeof(Debouncer))
                {
                    if (instance == null)
                    {
                        instance = new Debouncer();
                    }
                }
            }
            return instance;
        }

        private Timer timer;
        public int update_interval = 200;

        public void SetDebounceDelay(int milliseconds)
        {
            update_interval = milliseconds;
        }

        public void Debounce(Action action)
        {
            if (timer == null)
            {
                timer = new Timer((object state) =>
                {
                    action();
                    timer.Dispose();
                    timer = null;
                }, null, update_interval, Timeout.Infinite);
            }
        }
    }
}
