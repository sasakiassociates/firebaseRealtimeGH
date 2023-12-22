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
            // Handshake with the Firebase database
            _firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            parsedObjectList = new List<T>();
            parsedObjectName = typeof(T).Name.ToLower();

            // Clear the database of markers
            _firebaseClient.Child("bases").Child("test_proj").Child(parsedObjectName).DeleteAsync();

            if (_firebaseClient == null)
            {
                throw new Exception("Could not connect to Firebase");
            }
        }

        // This will be used to post data that the detector can read...not sure what yet though
        public async Task<T> PostAsync(T parsedObject)
        {
            var result = await _firebaseClient.Child("bases").Child("test_proj").Child(parsedObjectName).PostAsync<T>(parsedObject);
            parsedObject.uuid = result.Key;
            return parsedObject;
        }

        public async Task<List<T>> RetrieveAsync()
        {
            var result = await _firebaseClient.Child("bases").Child("test_proj").Child(parsedObjectName).OnceAsync<T>();
            
            foreach (var item in result)
            {
                parsedObjectList.Add(item.Object);
                item.Object.uuid = item.Key;
            }

            return parsedObjectList;
        }

        // TODO change this to work with the new data structure
        // Where does the project get assigned?
        public void Subscribe()
        {
            // Opens a new thread observing the database
            observable = _firebaseClient.Child("bases").Child("test_proj").Child(parsedObjectName).AsObservable<T>().Subscribe(dbEventHandler => onNewData(dbEventHandler));
            Console.WriteLine("Subscribed to database");
        }

        public void Unsubscribe()
        {
            // Ends the thread observing the database
            observable.Dispose();
            Console.WriteLine("Unsubscribed from database");
        }

        // Will launch for every object in the database changed or added
        public List<T> WaitForNewData(CancellationToken cancellationToken)
        {
            // Wait for the new data or cancellation
            WaitHandle.WaitAny(new WaitHandle[] { newInfoEvent, cancellationToken.WaitHandle });

            // Throw an exception if cancellation was requested
            //cancellationToken.ThrowIfCancellationRequested();

            return parsedObjectList;
        }

        private void onNewData(Firebase.Database.Streaming.FirebaseEvent<T> eventSource)
        {
            string uuid = eventSource.Key;

            if ((uuid == null) || (uuid == ""))
            {
                return;
            }

            int index = -1;
            foreach (T item in parsedObjectList)
            {
                if (item.uuid == uuid)
                {
                    //Console.WriteLine("Found " + uuid);
                    index = parsedObjectList.IndexOf(item);
                    break;
                }
            }

            Console.WriteLine("----------------------------");
            foreach (var item in parsedObjectList)
            {
                Console.WriteLine(item.uuid);
            }
            Console.WriteLine("----------------------------");

            /*Console.WriteLine("----------------------------");
            Console.WriteLine("Event type: " + eventSource.EventType);
            Console.WriteLine("Key: " + eventSource.Key);
            Console.WriteLine("Object: " + eventSource.Object);
            Console.WriteLine("Index: " + index);
            Console.WriteLine("----------------------------");*/

            // TODO currently the rhino component isn't deleting the former 
            if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
            {
                if (index != -1)
                {
                    T marker = parsedObjectList[index];
                    Console.WriteLine("Removing " + marker.uuid);
                    parsedObjectList.RemoveAt(index);
                }
            }
            else if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
            {
                if (index != -1)
                {
                    T marker = parsedObjectList[index];
                    //Console.WriteLine("Updating " + marker.uuid);
                    marker = eventSource.Object;
                }
                else
                {
                    T marker = eventSource.Object;
                    marker.uuid = eventSource.Key;
                    //Console.WriteLine("Added " + marker.uuid);
                    parsedObjectList.Add(marker);
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
