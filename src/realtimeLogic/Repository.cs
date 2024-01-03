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

        /*public async Task<List<T>> RetrieveAsync()
        {
            var result = await markerFolder.OnceAsync<T>();

            foreach (var item in result)
            {
                parsedObjectList.Add(item.Object);
                item.Object.uuid = item.Key;
            }

            return parsedObjectList;
        }
*/
        // TODO change this to work with the new data structure
        // Where does the project get assigned?
        public async Task SubscribeAsync()
        {
            // Clear the markers if it hasn't already
            // TODO this doesn't work in time for the subscribe function to 
            //await markerFolder.DeleteAsync();
            await markerFolder.PutAsync(listenerPlaceholder);

            //await PutAsync("{\"listening\": true}");
            // Opens a new thread observing the database
            observable = markerFolder.AsObservable<JObject>().Subscribe(dbEventHandler => onNewData(dbEventHandler), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine("Subscribed to database");
        }

        public async Task UnsubscribeAsync()
        {
            if (observable != null)
            {
                //await PutAsync("{\"listening\":false}");
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

        // Will launch for every object in the database changed or added
        public string WaitForNewData(CancellationToken cancellationToken)
        {

            // Wait for the new data or cancellation
            WaitHandle.WaitAny(new WaitHandle[] { newInfoEvent, cancellationToken.WaitHandle });

            // Throw an exception if cancellation was requested
            //cancellationToken.ThrowIfCancellationRequested();

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
            //Console.WriteLine("New data: " + eventSource.EventType + " " + eventSource.Key + " " + eventSource.Object.ToString());

            /*if ((uuid == null) || (uuid == ""))
            {
                return;
            }*/
            if (eventSource.Key == "listener")
            {
                return;
            }

            /*Console.WriteLine("----------------------------");
            foreach (var key in dataDictionary.Keys)
            {
                Console.WriteLine(key + ": " + dataDictionary[key]);
            }*/

            // TODO whenever a marker is deleted, the observer stops working
            if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
            {
                if (dataDictionary.ContainsKey(eventSource.Key))
                {
                    dataDictionary[eventSource.Key] = eventSource.Object.ToString();
                }
                else
                {
                    dataDictionary.Add(eventSource.Key, eventSource.Object.ToString());
                }
            }
            else if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(eventSource.Key))
                {
                    Console.WriteLine("Deleting " + eventSource.Key);
                    dataDictionary.Remove(eventSource.Key);
                }
            }

            incomingData = DictionaryToString(dataDictionary);

            /*Console.WriteLine(incomingData);*/

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
