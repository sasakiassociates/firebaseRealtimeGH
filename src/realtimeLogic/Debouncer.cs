using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
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
        public int update_interval = 1000;
        private Dictionary<string, Action> idActionPairs = new Dictionary<string, Action>();

        public void SetDebounceDelay(int milliseconds)
        {
            update_interval = milliseconds;
        }

        public void Debounce(string id, Action action, AutoResetEvent _event)
        {
            if (timer == null)
            {
                timer = new Timer((object state) =>
                {
                    foreach (KeyValuePair<string, Action> pair in idActionPairs)
                    {
                        pair.Value();
                    }
                    _event.Set();
                    timer.Dispose();
                    timer = null;
                }, null, update_interval, Timeout.Infinite);
            }
            
            if (idActionPairs.ContainsKey(id))
            {
                idActionPairs[id] = action;
            }
            else
            {
                idActionPairs.Add(id, action);
            }
        }
    }
}
