using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace realtimeLogic
{
    /// <summary>
    /// Listens to a particular folder in the database and executes a strategy when an update event is triggered
    /// </summary>
    public class DatabaseObserver
    {
        public EventHandlingStrategy strategy;
        ChildQuery observingFolder;
        FirebaseObject<string> listenerKey;
        public string project;
        IDisposable subscription;
        string folder;
        string listenerPlaceholder = "{ \"listener\": {\"status\": \"listening\"}}";

        public DatabaseObserver(FirebaseClient firebaseClient, string targetFolder, string projectName) 
        { 
            project = projectName;
            strategy = EventHandlerFactory.createStrategy(targetFolder);
            observingFolder = firebaseClient.Child("bases").Child(project).Child(targetFolder);
            folder = targetFolder;
        }

        public async Task Subscribe(AutoResetEvent updateEvent)
        {
            listenerKey = await observingFolder.PostAsync(listenerPlaceholder);
            subscription = observingFolder.AsObservable<JObject>().Subscribe(d => strategy.Parse(d, updateEvent), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine($"Subscribed to \"bases/{project}/{folder}\"");
        }

        public async Task Unsubscribe()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                await observingFolder.Child(listenerKey.Key).DeleteAsync();
                Console.WriteLine($"Unsubscribed from \"bases\"/{project}/{folder}\"");
            }
            else
            {
                Console.WriteLine("No subscription to unsubscribe from");
                return;
            }
        }
    }
}
