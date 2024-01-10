using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace realtimeLogic
{
    public class EventHandlerFactory
    {
        public static EventHandlingStrategy createStrategy(string targetFolder)
        {
            if (targetFolder == "markers")
            {
                return new MarkerStrategy();
            }
            else if (targetFolder == "config")
            {
                return new ConfigStrategy();
            }
            else
            {
                return null;
            }
        }
    }

    public interface EventHandlingStrategy
    {
        void Execute(FirebaseEvent<JObject> eventSource, AutoResetEvent updateEvent);
    }

    public class MarkerStrategy : EventHandlingStrategy
    {
        public void Execute(FirebaseEvent<JObject> eventSource, AutoResetEvent updateEvent)
        {
            Console.WriteLine("Marker updated");
            updateEvent.Set();
        }
    }

    public class ConfigStrategy : EventHandlingStrategy
    {
        public void Execute(FirebaseEvent<JObject> eventSource, AutoResetEvent updateEvent)
        {
            Console.WriteLine("Config updated");
            updateEvent.Set();
        }
    }
}
