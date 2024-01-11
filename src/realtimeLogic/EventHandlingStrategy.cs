using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace realtimeLogic
{
    public class EventHandlerFactory
    {
        public static EventHandlingStrategy createStrategy(string targetFolder)
        {
            if (targetFolder == "marker")
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
        DataManager dataManager { get; set; }

        void Parse(FirebaseEvent<JObject> eventSource, AutoResetEvent updateEvent);
    }

    public class MarkerStrategy : EventHandlingStrategy
    {
        public DataManager dataManager { get; set; }
        private Dictionary<string, string> dataDictionary = new Dictionary<string, string>();

        public MarkerStrategy()
        {
            dataManager = DataManager.GetInstance();
        }

        public void Parse(FirebaseEvent<JObject> eventSource, AutoResetEvent updateEvent)
        {
            string uuid = eventSource.Key;

            if (eventSource.EventType == FirebaseEventType.InsertOrUpdate)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    //Console.WriteLine($"Updating existing marker {uuid}");
                    dataDictionary[uuid] = eventSource.Object.ToString();
                }
                else
                {
                    //Console.WriteLine($"Adding new marker {uuid}");
                    dataDictionary.Add(uuid, eventSource.Object.ToString());
                }
            }
            else if (eventSource.EventType == FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    //Console.WriteLine($"Removing marker {uuid}");
                    dataDictionary.Remove(uuid);
                }
            }

            dataManager.Update("marker", dataDictionary);
            updateEvent.Set();
        }
    }

    public class ConfigStrategy : EventHandlingStrategy
    {
        public DataManager dataManager { get; set; }
        // default update interval is 1 second
        public int updateInterval = 1000;

        public ConfigStrategy()
        {
            dataManager = DataManager.GetInstance();
        }

        public void Parse(FirebaseEvent<JObject> eventSource, AutoResetEvent updateEvent)
        {
            string optionName = eventSource.Key;
            string optionValue = eventSource.Object.ToString();

            if (optionName == "updateInterval")
            {
                updateInterval = Int32.Parse(optionValue);
            }

            dataManager.Update("config", new Dictionary<string, string>() { { "updateInterval", updateInterval.ToString() } });

            updateEvent.Set();
        }
    }
}
