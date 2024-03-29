﻿using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
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

        /// <summary>
        /// Subscribes to the database and listens for updates to the target folder
        /// </summary>
        /// <param name="_updateEvent"></param>
        /// <returns></returns>
        public async Task Subscribe(AutoResetEvent _updateEvent)
        {
            updateEvent = _updateEvent;
            // Put a placeholder in the listeners folder to indicate that this observer is listening (subscribe only works when there is data in the folder)
            await observingFolder.Child("listeners").PutAsync(observerDataJson);
            
            InitialPull();

            subscription = observingFolder
                .AsObservable<JToken>()
                .Subscribe(_firebaseEvent =>
                {
                    Console.WriteLine($"Received event: {_firebaseEvent.EventType} {folderName} {_firebaseEvent.Key} {_firebaseEvent.Object}");
                    // Use the key (e.g., object ID) to identify each object
                    string objectId = _firebaseEvent.Key;
                    JToken data = _firebaseEvent.Object;

                    // TODO find a better way to handle these cases
                    if (objectId == "listeners")
                    {
                        return;
                    }
                    if (objectId == "update_interval")
                    {
                        // Convert the data to an int
                        int milliseconds = int.Parse(data.ToString());
                        debouncer.SetDebounceDelay(milliseconds);
                    }

                    ParseEvent(_firebaseEvent);

                    // TODO fix the Debounce function. Currently it misses updates frequently (could be Timer instantiation issue)
                    // Debouncer starts a timer that will wait to process the updates until the timer expires
                    debouncer.Debounce(() =>
                    {
                        updatedData = DictionaryToString();
                        updateEvent.Set();
                    });
                },
                ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine($"Subscribed to \"{folderName}\"");
        }

        /// <summary>
        /// The initial pull of data from the database
        /// </summary>
        private async void InitialPull()
        {
            var initialData = await observingFolder.OnceAsync<JToken>();

            foreach (var data in initialData)
            {
                if (data.Key == "listeners")
                {
                    continue;
                }
                if (data.Key == "update_interval")
                {
                    Console.WriteLine("Update interval changed");
                    int milliseconds = int.Parse(data.Object.ToString());
                    debouncer.SetDebounceDelay(milliseconds);
                }
                // Ensure there are quotes around string objects
                var settings = new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                };

                string dataJson = JsonConvert.SerializeObject(data.Object, settings);
                Console.WriteLine($"Initial pull: {data.Key} {dataJson}");
                ParseDatapoint(data.Key, dataJson);
            }

            updatedData = DictionaryToString();

            updateEvent.Set();
        }

        /// <summary>
        /// Unsubscribe from the database and remove the listener from the listeners folder
        /// </summary>
        public void Unsubscribe()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                _ = observingFolder.Child("listeners").Child(observerId).DeleteAsync();
                Console.WriteLine($"Unsubscribed from \"{folderName}\"");
            }
            else
            {
                Console.WriteLine("No subscription to unsubscribe from");
                return;
            }
        }

        /// <summary>
        /// This method is used to parse the data from the database and store it in the dataDictionary (called in the initial pull)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public void ParseDatapoint(string key, string data)
        {
            if (dataDictionary.ContainsKey(key))
            {
                dataDictionary[key] = data.ToString();
            }
            else
            {
                dataDictionary.Add(key, data.ToString());
            }
        }

        /// <summary>
        /// Parses the firebase event and updates the dataDictionary accordingly
        /// </summary>
        /// <param name="_firebaseEvent"></param>
        public void ParseEvent(FirebaseEvent<JToken> _firebaseEvent)
        {
            string key = _firebaseEvent.Key;
            if (key == null || key == "")
            {
                Console.WriteLine("No UUID");
                return;
            }

            if (_firebaseEvent.EventType == FirebaseEventType.InsertOrUpdate)
            {
                // TODO we need to get to another level down from here because it's just updating the whole folder
                if (dataDictionary.ContainsKey(key))
                {
                    Console.WriteLine($"Updating existing data for {key}");
                    dataDictionary[key] = _firebaseEvent.Object.ToString();
                }
                else
                {
                    Console.WriteLine($"Adding new data for {key}");
                    dataDictionary.Add(key, _firebaseEvent.Object.ToString());
                }
            }
            else if (_firebaseEvent.EventType == FirebaseEventType.Delete)
            {
                if (dataDictionary.ContainsKey(key))
                {
                    Console.WriteLine($"Deleting data for {key}");
                    dataDictionary.Remove(key);
                }
            }
        }


        /// <summary>
        /// Converts the dataDictionary to a string
        /// </summary>
        /// <returns></returns>
        private string DictionaryToString()
        {
            if (dataDictionary.Count == 0)
            {
                return null;
            }

            string output = "{\n";
            // TODO this enumeration gets interrupted by new data coming in, so it's not thread safe
            foreach (var key in dataDictionary.Keys)
            {
                output += $" \"{key}\": {dataDictionary[key]},\n";
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
