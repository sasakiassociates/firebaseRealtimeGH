﻿using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        Debouncer debouncer = Debouncer.GetInstance();
        private object latestUpdatesLock = new object();

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
            // Put a placeholder in the listeners folder to indicate that this observer is listening (subscribe only works when there is data in the folder)
            await observingFolder.Child("listeners").PutAsync(observerDataJson);
            
            InitialPull();

            subscription = observingFolder
                .AsObservable<JObject>()
                .Subscribe(_firebaseEvent =>
                {
                    // Use the key (e.g., object ID) to identify each object
                    string objectId = _firebaseEvent.Key;

                    if (objectId == "listeners")
                    {
                        return;
                    }

                    if (objectId == "update_interval")
                    {
                        Console.WriteLine("Update interval changed");
                        int milliseconds = _firebaseEvent.Object.ToObject<int>();
                        debouncer.SetDebounceDelay(milliseconds);
                        return;
                    }

                    // Debouncer starts a timer that will wait to process the updates until the timer expires
                    // TODO this needs to be changed to allow for multiple observers to run updates
                    Console.WriteLine($"Received update from \"{folderName}\"");
                    debouncer.Debounce(objectId, async () =>
                    {
                        await ParseEventAsync(_firebaseEvent);
                    }, _updateEvent);
                },
                ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine($"Subscribed to \"{folderName}\"");
        }

        private async void InitialPull()
        {
            var initialData = await observingFolder.OnceAsync<JToken>();

            foreach (var data in initialData)
            {
                if (data.Key == "listeners")
                {
                    continue;
                }

                string dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(data.Object);
                Console.WriteLine($"Initial pull: {data.Key} {dataJson}");
                await ParseDatapointAsync(data.Key, dataJson);
            }
        }

        private async Task ProcessBatchedChangesAsync(List<FirebaseEvent<JObject>> batchedChanges)
        {
            foreach (FirebaseEvent<JObject> _firebaseEvent in batchedChanges)
            {
                await ParseEventAsync(_firebaseEvent);
            }

            updateEvent.Set();
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

        public async Task ParseDatapointAsync(string uuid, string data)
        {
            if (dataDictionary.ContainsKey(uuid))
            {
                Console.WriteLine($"Updating existing data for {uuid}");
                dataDictionary[uuid] = data;
            }
            else
            {
                Console.WriteLine($"Adding new data for {uuid}");
                dataDictionary.Add(uuid, data);
            }

            updatedData = DictionaryToString(dataDictionary);

            updateEvent.Set();
        }

        public async Task ParseEventAsync(FirebaseEvent<JObject> _firebaseEvent)
        {
            string uuid = _firebaseEvent.Key;

            if (_firebaseEvent.EventType == FirebaseEventType.InsertOrUpdate)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    Console.WriteLine($"Updating existing data for {uuid}");
                    dataDictionary[uuid] = _firebaseEvent.Object.ToString();
                }
                else
                {
                    Console.WriteLine($"Adding new data for {uuid}");
                    dataDictionary.Add(uuid, _firebaseEvent.Object.ToString());
                }
            }
            else if (_firebaseEvent.EventType == FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(uuid))
                {
                    Console.WriteLine($"Deleting data for {uuid}");
                    dataDictionary.Remove(uuid);
                }
            }

            updatedData = DictionaryToString(dataDictionary);
        }

        private string DictionaryToString(Dictionary<string, string> dictionary)
        {
            string output = "{\n";
            // TODO this enumeration gets interrupted by new data coming in, so it's not thread safe
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
