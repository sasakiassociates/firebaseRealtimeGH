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

namespace realtimeLogic
{
    public class Repository
    {
        private static readonly object lockObject = new object();
        private static readonly Repository instance;
        private ChildQuery markerFolder;

        private DateTime lastUpdate = DateTime.Now;
        private double updateInterval = 2;

        // Firebase subscribe only works if there is at least one object in the specified folder
        // This object will hold info about the program that is currently subscibed
        // TODO add subsciber info to this object
        string listenerPlaceholder = "{ \"listener\": {\"status\": \"listening\"}}";

        public static Repository GetInstance(string pathToKeyFile, string firebaseUrl)
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    return new Repository(pathToKeyFile, firebaseUrl);
                }
                else
                {
                    return instance;
                }
            }
        }
        private readonly FirebaseClient _firebaseClient;
        private AutoResetEvent newInfoEvent = new AutoResetEvent(false);
        public Dictionary<string, string> dataDictionary = new Dictionary<string, string>();
        public string incomingData;
        
        public IDisposable observable { get; set; }

        private Repository(string pathToKeyFile, string firebaseUrl)
        {
            // Handshake with the Firebase database
            _firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });

            if (_firebaseClient == null)
            {
                throw new Exception("Could not connect to Firebase");
            }

            markerFolder = _firebaseClient.Child("bases").Child("test_proj").Child("marker");
        }

        // This will be used to post data that the detector can read...not sure what yet though
        // Post Async puts the message under a unique identifier key
        public async Task PostAsync(string message)
        {
            var post = await markerFolder.PostAsync(message);
            Console.WriteLine(post.Key);
        }

        public async Task PutAsync(string message)
        {
            await markerFolder.PutAsync(message);
        }

        public async Task DeleteAsync(string uuid)
        {
            await markerFolder.Child(uuid).DeleteAsync();
        }

        // Where does project get assigned?
        public async Task SubscribeAsync()
        {
            await markerFolder.PutAsync(listenerPlaceholder);

            observable = markerFolder.AsObservable<JObject>().Subscribe(dbEventHandler => onNewData(dbEventHandler), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine("Subscribed to database");
        }

        public async Task UnsubscribeAsync()
        {
            if (observable != null)
            {
                observable.Dispose();
                observable = null; // Set observable to null to indicate that it's disposed.
                Console.WriteLine("Unsubscribed from database");
                await markerFolder.Child("listener").DeleteAsync();
            }
            else
            {
                Console.WriteLine("Already unsubscribed");
            }
        }

        // Currently runs for every change in the database, need to batch
        public string WaitForNewData(CancellationToken cancellationToken)
        {
            WaitHandle.WaitAny(new WaitHandle[] { newInfoEvent, cancellationToken.WaitHandle });
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
        private void onNewData(Firebase.Database.Streaming.FirebaseEvent<JObject> eventSource)
        {
            if (eventSource.Key == "listener")
            {
                return;
            }
            if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
            {
                if (dataDictionary.ContainsKey(eventSource.Key))
                {
                    //Console.WriteLine("Updating " + eventSource.Key);
                    dataDictionary[eventSource.Key] = eventSource.Object.ToString();
                }
                else
                {
                    //Console.WriteLine("Adding " + eventSource.Key);
                    dataDictionary.Add(eventSource.Key, eventSource.Object.ToString());
                }
            }
            else if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(eventSource.Key))
                {
                    //Console.WriteLine("Deleting " + eventSource.Key);
                    dataDictionary.Remove(eventSource.Key);
                }
            }

            incomingData = DictionaryToString(dataDictionary);
            Console.WriteLine(incomingData);

            // this debouncing should probably be done sooner, but it's here for now so that we can ensure we get all updates
            // TODO if the last update wasn't perfectly timed then it won't update. Need to move this check somewhere else
            if (DateTime.Now.Subtract(lastUpdate).TotalSeconds > updateInterval)
            {
                Console.WriteLine(DateTime.Now.Subtract(lastUpdate).TotalSeconds);
                // Continues any thread currently waiting for new data via the "WaitForNewData" function
                newInfoEvent.Set();
                lastUpdate = DateTime.Now;
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
