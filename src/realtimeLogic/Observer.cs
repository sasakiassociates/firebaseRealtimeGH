using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
        string observerId = Guid.NewGuid().ToString();
        string observerDataJson;

        public DatabaseObserver(FirebaseClient firebaseClient, string targetFolder, string projectName) 
        { 
            project = projectName;
            strategy = EventHandlerFactory.createStrategy(targetFolder);
            observingFolder = firebaseClient.Child("bases").Child(project).Child(targetFolder);
            folder = targetFolder;
            observerDataJson = $"{{\"{observerId}\": {{\"status\" : \"listening\"}}}}";
        }

        public async Task Subscribe(AutoResetEvent updateEvent)
        {
            await observingFolder.Child("listeners").PutAsync(observerDataJson);
            subscription = observingFolder.AsObservable<JObject>().Subscribe(d => strategy.Parse(d, updateEvent), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine($"Subscribed to \"bases/{project}/{folder}\"");
        }

        public async Task Unsubscribe()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                await observingFolder.Child("listeners").Child(observerId).DeleteAsync();
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
