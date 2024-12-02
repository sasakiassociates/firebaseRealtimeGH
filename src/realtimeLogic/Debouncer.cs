using System;
using System.Threading;

namespace realtimeLogic
{
    public class Debouncer
    {
        Logger _logger;
        private Timer timer;
        public int update_interval = 33;

        public Debouncer() 
        {
            _logger = Logger.GetInstance();
            _logger.Log(this, "Initialized");
        }

        public void SetDebounceDelay(int milliseconds)
        {
            update_interval = milliseconds;
        }

        public void Debounce(Action action)
        {
            try
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
            catch (Exception e)
            {
                _logger.Log(this, "Error debouncing: " + e.Message);
            }
        }
    }
}