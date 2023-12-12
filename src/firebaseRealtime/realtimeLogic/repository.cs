using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;

namespace realtimeLogic
{
    public class Repository<T> where T : parsedObject
    {
        private static readonly object lockObject = new object();
        private static readonly Repository<T> instance;
        public static Repository<T> GetInstance
        {
            get
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        return new Repository<T>();
                    }
                    else
                    {
                        return instance;
                    }
                }
            }
        }
        private const string FirebaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
        private readonly FirebaseClient _firebaseClient;
        private const string pathToJsonFile = @"C:\Users\nshikada\Documents\GitHub\firebaseRealtimeGH\keys\firebase_table-key.json";
        public List<T> parsedObjectList { get; set; }
        private string parsedObjectName { get; set; }
        private AutoResetEvent newInfoEvent = new AutoResetEvent(false);

        private IDisposable observable { get; set; }

        private Repository()
        {
            _firebaseClient = new FirebaseClient(FirebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(), AsAccessToken = true });
            parsedObjectList = new List<T>();
            parsedObjectName = typeof(T).Name.ToLower();
        }

        public async Task RetrieveAsync()
        {
            var result = await _firebaseClient.Child(parsedObjectName).OnceAsync<T>();
            
            foreach (var item in result)
            {
                parsedObjectList.Add(item.Object);
                item.Object.uuid = item.Key;
            }
        }

        public void Subscribe()
        {
            // Opens a new thread observing the database
            observable = _firebaseClient.Child(parsedObjectName).AsObservable<T>().Subscribe(dbEventHandler => onNewData(dbEventHandler));
            Console.WriteLine("Subscribed to database");
        }

        public void Unsubscribe()
        {
            // Ends the thread observing the database
            observable.Dispose();
            Console.WriteLine("Unsubscribed from database");
        }

        public List<T> WaitForNewData(CancellationToken cancellationToken)
        {
            // Wait for the new data or cancellation
            WaitHandle.WaitAny(new WaitHandle[] { newInfoEvent, cancellationToken.WaitHandle });

            // Check if cancellation is requested
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine("New data received");
            return parsedObjectList;
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

            // TODO Make this run only once after all the changes have been made
            // Continues any thread currently waiting for new data via the "WaitForNewData" function
            newInfoEvent.Set();
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
