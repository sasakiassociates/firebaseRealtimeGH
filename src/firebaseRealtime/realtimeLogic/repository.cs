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
        public static Repository<T> GetInstance(string pathToKeyFile, string firebaseUrl)
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    return new Repository<T>(pathToKeyFile, firebaseUrl);
                }
                else
                {
                    return instance;
                }
            }
        }
        private readonly FirebaseClient _firebaseClient;
        public List<T> parsedObjectList { get; set; }
        private string parsedObjectName { get; set; }
        private AutoResetEvent newInfoEvent = new AutoResetEvent(false);

        private IDisposable observable { get; set; }

        private Repository(string pathToKeyFile, string firebaseUrl)
        {
            _firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            parsedObjectList = new List<T>();
            parsedObjectName = typeof(T).Name.ToLower();

            if (_firebaseClient == null)
            {
                throw new Exception("Could not connect to Firebase");
            }
        }

        public async Task<List<T>> RetrieveAsync()
        {
            var result = await _firebaseClient.Child(parsedObjectName).OnceAsync<T>();
            
            foreach (var item in result)
            {
                parsedObjectList.Add(item.Object);
                item.Object.uuid = item.Key;
            }

            return parsedObjectList;
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

            // Throw an exception if cancellation was requested
            //cancellationToken.ThrowIfCancellationRequested();

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

        private async Task<string> GetAccessToken(string pathToKeyFile)
        {
            var credential = GoogleCredential.FromFile(pathToKeyFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

    }
}
