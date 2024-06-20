using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public class LogEventArgs : EventArgs
    {
        public string Message { get; }

        public LogEventArgs(string message)
        {
            Message = message;
        }
    }
}
