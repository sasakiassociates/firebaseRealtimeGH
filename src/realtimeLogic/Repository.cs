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

namespace realtimeLogic
{
    public class Repository
    {
        private static readonly object lockObject = new object();
        private static readonly Repository instance;
        private ChildQuery markerFolder;
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
        
        private IDisposable observable { get; set; }

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
        /*public async Task PostAsync(T parsedObject)
        {
            var result = await markerFolder.PostAsync(parsedObject);
            parsedObject.uuid = result.Key;
            return parsedObject;
        }

        public async Task<List<T>> RetrieveAsync()
        {
            var result = await markerFolder.OnceAsync<T>();
            
            foreach (var item in result)
            {
                parsedObjectList.Add(item.Object);
                item.Object.uuid = item.Key;
            }

            return parsedObjectList;
        }*/

        // TODO change this to work with the new data structure
        // Where does the project get assigned?
        public void Subscribe()
        {
            // Clear the markers if it hasn't already
            // TODO this doesn't work in time for the subscribe function to 
            markerFolder.DeleteAsync();

            // Wait for the database to clear
            Thread.Sleep(200);
            // Opens a new thread observing the database
            observable = markerFolder.AsObservable<JObject>().Subscribe(dbEventHandler => onNewData(dbEventHandler));
            Console.WriteLine("Subscribed to database");
        }

        public void Unsubscribe()
        {
            // Ends the thread observing the database
            observable.Dispose();
            Console.WriteLine("Unsubscribed from database");
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
            string uuid = eventSource.Key;

            if ((uuid == null) || (uuid == ""))
            {
                return;
            }

            /*Console.WriteLine("----------------------------");
            foreach (var key in dataDictionary.Keys)
            {
                Console.WriteLine(key + ": " + dataDictionary[key]);
            }*/

            // TODO currently the rhino component isn't deleting the former 
            if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(eventSource.Key))
                {
                    dataDictionary.Remove(eventSource.Key);
                }
            }
            else if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
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

            incomingData = DictionaryToString(dataDictionary);

            Console.WriteLine(incomingData);

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
