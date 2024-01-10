using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Collections;
using static System.Collections.Specialized.BitVector32;
using Firebase.Database.Streaming;

namespace realtimeLogic
{
    public class OldRepository
    {
        private static readonly object lockObject = new object();
        private static readonly OldRepository instance;
        private ChildQuery markerFolder;
        private ChildQuery configFolder;

        private readonly Firebase.Database.FirebaseClient _firebaseClient;
        private AutoResetEvent newInfoEvent = new AutoResetEvent(false);
        public Dictionary<string, string> dataDictionary = new Dictionary<string, string>();
        public string incomingData;

        // Objects used to batch updates
        public Dictionary<string, string> batchDictionary = new Dictionary<string, string>();
        private readonly object batchLock = new object();
        Queue updateQueue = new Queue();
        Queue deletionQueue = new Queue();

        // Update interval in milliseconds to be updated via config in the database later (default 100ms)
        private int updateInterval = 100;

        // Firebase subscribe only works if there is at least one object in the specified folder
        // This object will hold info about the program that is currently subscibed
        // TODO add subsciber info to this object
        string listenerPlaceholder = "{ \"listener\": {\"status\": \"listening\"}}";
        FirebaseObject<string> configListenerKey;
        FirebaseObject<string> markerListenerKey;
        public IDisposable markerObserver { get; set; }
        public IDisposable configObserver { get; set; }

        public static OldRepository GetInstance(string pathToKeyFile, string firebaseUrl)
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    return new OldRepository(pathToKeyFile, firebaseUrl);
                }
                else
                {
                    return instance;
                }
            }
        }
        

        private OldRepository(string pathToKeyFile, string firebaseUrl)
        {
            // Handshake with the Firebase database
            _firebaseClient = new Firebase.Database.FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });

            if (_firebaseClient == null)
            {
                throw new Exception("Could not connect to Firebase");
            }

            markerFolder = _firebaseClient.Child("bases").Child("test_proj").Child("marker");
            configFolder = _firebaseClient.Child("bases").Child("test_proj").Child("config");
        }

        // This will be used to post data that the detector can read...not sure what yet though
        // Post Async puts the message under a unique identifier key
        public async Task PostAsync(string destination, string message)
        {
            ChildQuery targetFolder = _firebaseClient.Child("bases").Child("test_proj").Child(destination);
            var post = await targetFolder.PostAsync(message);
            Console.WriteLine(post.Key);
        }

        private async Task PullConfigData()
        {
            var configData = await configFolder.OnceSingleAsync<Dictionary<string, string>>();
            updateInterval = Int32.Parse(configData["updateInterval"]);
            Console.WriteLine("Update interval set to " + updateInterval);
        }

        public async Task PutAsync(string message, string destination = "marker")
        {
            ChildQuery targetFolder = _firebaseClient.Child("bases").Child("test_proj").Child(destination);
            await targetFolder.PutAsync(message);
        }

        public async Task DeleteAsync(string target, string destination = "marker")
        {
            ChildQuery targetFolder = _firebaseClient.Child("bases").Child("test_proj").Child(destination);
            await targetFolder.Child(target).DeleteAsync();
        }

        // Where does project get assigned?
        public async Task SubscribeAsync()
        {
            // Pull the config data from the database
            await PullConfigData();

            markerListenerKey = await markerFolder.PostAsync(listenerPlaceholder);
            configListenerKey = await configFolder.PostAsync(listenerPlaceholder);

            configObserver = configFolder.AsObservable<JObject>().Subscribe(dbEventHandler => onNewConfigData(dbEventHandler), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            markerObserver = markerFolder.AsObservable<JObject>().Subscribe(dbEventHandler => onNewData(dbEventHandler), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine("Subscribed to database");
        }

        public async Task UnsubscribeAsync()
        {
            if (markerObserver != null)
            {
                markerObserver.Dispose();
                markerObserver = null; // Set observable to null to indicate that it's disposed.
                Console.WriteLine("Unsubscribed from marker database");
                await markerFolder.Child(markerListenerKey.Key).DeleteAsync();
            }
            else
            {
                Console.WriteLine("Already unsubscribed");
            }

            if (configObserver != null)
            {
                configObserver.Dispose();
                configObserver = null; // Set observable to null to indicate that it's disposed.
                Console.WriteLine("Unsubscribed from config database");
                await configFolder.Child(configListenerKey.Key).DeleteAsync();
            }
            else
            {
                Console.WriteLine("Already unsubscribed");
            }
        }

        // Currently runs for every change in the database, need to batch
        public string WaitForNewData(CancellationToken cancellationToken)
        {
            // Waits until there is an update to the database
            WaitHandle.WaitAny(new WaitHandle[] { newInfoEvent, cancellationToken.WaitHandle });
            // When there is an update, wait for the updateInterval to pass
            Thread.Sleep(updateInterval);
            // Then process the queue of updates
            if (updateQueue.Count > 0)
            {
                lock (batchLock)
                {
                    foreach (Action action in updateQueue)
                    {
                        // Process the update
                        action();
                    }
                    updateQueue.Clear();
                }
            }

            if (deletionQueue.Count > 0)
            {
                lock (batchLock)
                {
                    foreach (Action action in deletionQueue)
                    {
                        // Process the update
                        action();
                    }
                    deletionQueue.Clear();
                }
            }

            //incomingData = DictionaryToString(dataDictionary);
            incomingData = DictionaryToString(batchDictionary);

            return incomingData;
        }

        private string DictionaryToString(Dictionary<string, string> dictionary)
        {
            string output = "{\n";
            foreach (var key in dictionary.Keys)
            {
                output += $" \"{key}\": {dictionary[key]},\n";
            }

            // Remove the trailing comma and newline, if any
            if (output.EndsWith(",\n"))
            {
                output = output.Substring(0, output.Length - 2) + "\n";
            }
            output += "}";

            return output;
        }

        //  We want to parse this data back into a string to output so we can use it however we want in later components
        private void onNewData(FirebaseEvent<JObject> eventSource)
        {
            string uuid = eventSource.Key;
            Action action = null;
            if (uuid == "listener")
            {
                return;
            }
            if (eventSource.EventType == FirebaseEventType.InsertOrUpdate)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    action = () => batchDictionary[uuid] = eventSource.Object.ToString();
                    //Console.WriteLine("Updating " + eventSource.Key);
                    dataDictionary[uuid] = eventSource.Object.ToString();
                }
                else
                {
                    action = () => batchDictionary.Add(uuid, eventSource.Object.ToString());
                    //Console.WriteLine("Adding " + eventSource.Key);
                    dataDictionary.Add(uuid, eventSource.Object.ToString());
                }
                lock (batchLock)
                {
                    updateQueue.Enqueue(action);
                }
            }
            else if (eventSource.EventType == FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    action = () => batchDictionary.Remove(uuid);
                    //Console.WriteLine("Deleting " + eventSource.Key);
                    dataDictionary.Remove(uuid);
                }
                lock (batchLock)
                {
                    deletionQueue.Enqueue(action);
                }
            }

            //incomingData = DictionaryToString(dataDictionary);

            newInfoEvent.Set();
        }

        private void onNewConfigData(FirebaseEvent<JObject> eventSource)
        {
            int incomingUpdateInterval = Int32.Parse(eventSource.Object["updateInterval"].ToString());
            if (incomingUpdateInterval != updateInterval)
            {
                updateInterval = incomingUpdateInterval;
                Console.WriteLine("Update interval changed to " + updateInterval);
            }
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
