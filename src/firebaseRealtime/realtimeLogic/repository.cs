using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reactive;

namespace realtimeLogic
{
    public class Repository<T> where T : parsedObject
    {
        public static readonly Repository<T> Instance = new Repository<T>();
        private const string FirebaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
        private readonly FirebaseClient _firebaseClient;
        private const string pathToJsonFile = @"C:\Users\nshikada\Documents\GitHub\firebaseRealtimeGH\keys\firebase_table-key.json";
        public List<T> parsedObjectList { get; set; }
        private string parsedObjectName { get; set; }

        private IDisposable observable { get; set; }
        private readonly List<IObserver> observers = new List<IObserver>();
        private readonly object lockObject = new object();
        private string state;

        public void RegisterObserver(IObserver observer)
        {
            lock (lockObject)
            {
                observers.Add(observer);
            }
        }

        public void RemoveObserver(IObserver observer)
        {
            lock (lockObject)
            {
                observers.Remove(observer);
            }
        }

        public void NotifyObservers()
        {
            lock (lockObject)
            {
                foreach (var observer in observers)
                {
                    observer.Update(state);
                }
            }
        }

        private Repository()
        {
            _firebaseClient = new FirebaseClient(FirebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(), AsAccessToken = true });
            parsedObjectList = new List<T>();
            parsedObjectName = typeof(T).Name.ToLower();
        }

        public void Retrieve()
        {
            var result = _firebaseClient.Child("markers").OnceAsync<T>();
            
            foreach (var item in result.Result)
            {
                item.Object.uuid = item.Key;
            }

            Console.WriteLine(result.Result.Count);
        }

        public void Subscribe()
        {
            // Opens a new thread observing the database
            observable = _firebaseClient.Child(parsedObjectName).AsObservable<T>().Subscribe(dbEventHandler => onNewData(dbEventHandler));
            Console.WriteLine("Subscribed to database");
        }

        public void EndSubscription()
        {
            // Ends the thread observing the database
            observable.Dispose();
            Console.WriteLine("Unsubscribed from database");
        }

        // This runs for every single change in the database
        private void onNewData(Firebase.Database.Streaming.FirebaseEvent<T> eventSource)
        {
            if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
            {
                if (parsedObjectList.Exists(x => x.uuid == eventSource.Key))
                {
                    var index = parsedObjectList.FindIndex(x => x.uuid == eventSource.Key);
                    parsedObjectList[index] = eventSource.Object;
                }
                else
                {
                    T marker = eventSource.Object;
                    marker.uuid = eventSource.Key;
                    parsedObjectList.Add(marker);

                    Console.WriteLine("New object added");
                    Console.WriteLine(eventSource.Object.uuid);
                }
            }
            else if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
            {
                if (parsedObjectList.Exists(x => x.uuid == eventSource.Key))
                {
                    var index = parsedObjectList.FindIndex(x => x.uuid == eventSource.Key);
                    parsedObjectList.RemoveAt(index);
                }
            }

            // TODO Somehow make this rerun the component?
        }

        private async Task<string> GetAccessToken()
        {
            var credential = GoogleCredential.FromFile(pathToJsonFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

    }
}
