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
        ChildQuery observingFolder;
        IDisposable subscription;
        public string folderName;
        string observerId = Guid.NewGuid().ToString();
        string observerDataJson;
        private Dictionary<string, string> dataDictionary = new Dictionary<string, string>();
        public string updatedData;
        AutoResetEvent updateEvent;
        FirebaseClient firebaseClient;

        public DatabaseObserver(FirebaseClient _firebaseClient, string targetFolder) 
        { 
            firebaseClient = _firebaseClient;
            // folder name is the last part of the target folder string
            foreach (string folder in targetFolder.Split('/'))
            {
                folderName = folder;
            }
            observingFolder = StringToFolder(targetFolder);
            observerDataJson = $"{{\"{observerId}\": {{\"status\" : \"listening\"}}}}";
        }

        public async Task Subscribe(AutoResetEvent _updateEvent)
        {
            updateEvent = _updateEvent;
            await observingFolder.Child("listeners").PutAsync(observerDataJson);
            subscription = observingFolder.AsObservable<JObject>().Subscribe(d => Parse(d), ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine($"Subscribed to \"{folderName}\"");
        }

        public async Task Unsubscribe()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                await observingFolder.Child("listeners").Child(observerId).DeleteAsync();
                Console.WriteLine($"Unsubscribed from \"{folderName}\"");
            }
            else
            {
                Console.WriteLine("No subscription to unsubscribe from");
                return;
            }
        }

        public void Parse(FirebaseEvent<JObject> eventSource)
        {
            string uuid = eventSource.Key;

            if (uuid == "listeners")
            {
                return;
            }

            if (eventSource.EventType == FirebaseEventType.InsertOrUpdate)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    //Console.WriteLine($"Updating existing marker {uuid}");
                    dataDictionary[uuid] = eventSource.Object.ToString();
                }
                else
                {
                    //Console.WriteLine($"Adding new marker {uuid}");
                    dataDictionary.Add(uuid, eventSource.Object.ToString());
                }
            }
            else if (eventSource.EventType == FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    //Console.WriteLine($"Removing marker {uuid}");
                    dataDictionary.Remove(uuid);
                }
            }

            updatedData = DictionaryToString(dataDictionary);
            
            updateEvent.Set();
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

        /// <summary>
        /// Takes in the string of the desired folder and returns a ChildQuery object pointing to that folder on Firebase
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private ChildQuery StringToFolder(string targetFolder)
        {
            // Separate string into parent and child folders by splitting at the last slash
            // "parent/child/folder1" -> ["parent", "child", "folder1"]
            string[] folderArray = targetFolder.Split('/');
            ChildQuery folderQuery = null;
            foreach (string folderName in folderArray)
            {
                if (folderQuery == null)
                {
                    folderQuery = firebaseClient.Child(folderName);
                }
                else
                {
                    folderQuery = folderQuery.Child(folderName);
                }
            }
            return folderQuery;
        }
    }
}
