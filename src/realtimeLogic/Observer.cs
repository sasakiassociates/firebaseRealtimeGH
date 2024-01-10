using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace realtimeLogic
{
    /// <summary>
    /// Listens to a particular folder in the database and executes a strategy when an update event is triggered
    /// </summary>
    public class DatabaseObserver
    {
        public EventHandlingStrategy strategy;
        public string target;
        public string project;
        IDisposable subscription;
        FirebaseObject<string> listenerKey;
        string listenerPlaceholder = "\"listening\": true";

        public DatabaseObserver(string targetFolder, string projectName) 
        { 
            target = targetFolder;
            project = projectName;
            strategy = EventHandlerFactory.createStrategy(targetFolder);
        }

        public async void Subscribe(FirebaseClient firebaseClient, AutoResetEvent updateEvent)
        {
            Console.WriteLine("Subscribed");
            
            ChildQuery query = firebaseClient.Child("bases").Child(project).Child(target);
            listenerKey = await query.PostAsync(listenerPlaceholder);
            subscription = firebaseClient.Child(target).AsObservable<JObject>().Subscribe(d => strategy.Execute(d, updateEvent));
        }

        public void Unsubscribe()
        {
            Console.WriteLine("Unsubscribed");
            subscription.Dispose();
        }
    }
}
