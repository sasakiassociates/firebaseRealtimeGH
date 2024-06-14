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
        IDisposable subscription;
        public string folderName;
        public Action<Dictionary<string, object>> callback;
        string observerId = Guid.NewGuid().ToString();
        string observerDataJson;
        private Dictionary<string, object> dataDictionary = new Dictionary<string, object>();
        public string updatedData;
        ChildQuery observingFolder;

        // Used for the checking thread
        private bool isListening = false;
        // The frequency with which the observer checks the database for updates we may have missed
        private int checkingInterval = 10000;

        Debouncer debouncer = Debouncer.GetInstance();

        public DatabaseObserver(ChildQuery _observingFolder, string _folderName = "") 
        { 
            folderName = _folderName;
            observingFolder = _observingFolder.Child(folderName);
            observerDataJson = "{{\"status\" : \"listening\"}}";
            observerDataJson = JsonConvert.SerializeObject(observerDataJson);
        }

        public async Task Reload()
        {
            UnsubscribeAsync();
            await Subscribe(callback);
        }

        /// <summary>
        /// Subscribes to the database and listens for updates to the target folder
        /// </summary>
        /// <param name="_updateEvent"></param>
        /// <returns></returns>
        public async Task Subscribe(Action<Dictionary<string, object>> _callback)
        {
            callback = _callback;
            
            // Put a placeholder in the listeners folder to indicate that this observer is listening (subscribe only works when there is data in the folder)
            await observingFolder.Child($"listeners/{observerId}").PutAsync(observerDataJson);
            
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
                        /*updatedData = GetData();*/
                        // copy the dataDictionary
                        _callback(dataDictionary);
                    });
                },
                ex => Console.WriteLine($"Observer error: {ex.Message}"));
            Console.WriteLine($"Subscribed to \"{folderName}\"");
            isListening = true;
            //_ = Task.Run(async () => { await CheckingThread(); });

        }

        /// <summary>
        /// Unsubscribe from the database and remove the listener from the listeners folder
        /// </summary>
        public async Task UnsubscribeAsync()
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

            isListening = false;
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
                    // Check if the is_deleted boolean is true and remove the data from the dictionary
                    JObject data = JObject.Parse(_firebaseEvent.Object.ToString());
                    if (data.ContainsKey("is_deleted"))
                    {
                        if (data["is_deleted"].ToObject<bool>())
                        {
                            Console.WriteLine($"Deleting data for {key}");
                            dataDictionary.Remove(key);
                        }
                    }
                }
                else
                {
                    JObject data = JObject.Parse(_firebaseEvent.Object.ToString());
                    if (data.ContainsKey("is_deleted"))
                    {
                        if (!data["is_deleted"].ToObject<bool>())
                        {
                            dataDictionary.Add(key, _firebaseEvent.Object.ToString());
                        }
                    }
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
        /// Periodically checks the database to see if we missed any updates
        /// </summary>
        /// <returns></returns>
        public async Task CheckingThread()
        {
            while (isListening)
            {
                // Wait for the checking interval
                await Task.Delay(checkingInterval);
                bool hasUpdates = false;
                // pull the data from the database
                var response = await observingFolder.OnceAsJsonAsync();
                if (response != null)
                {
                    JObject data = JObject.Parse(response.ToString());
                    // check if the data is different from the local data
                    foreach (var item in data)
                    {
                        if (dataDictionary.ContainsKey(item.Key))
                        {
                            if (dataDictionary[item.Key] != item.Value.ToString())
                            {
                                // update the local data
                                dataDictionary[item.Key] = item.Value.ToString();
                                hasUpdates = true;
                            }
                        }
                        else
                        {
                            // add the new data to the local data
                            dataDictionary.Add(item.Key, item.Value.ToString());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies the data dictionary, locks the thread while copying, and returns the copy as a string
        /// </summary>
        /// <returns></returns>
        public string GetData()
        {
            Dictionary<string, object> snapshot;
            lock (dataDictionary)
            {
                snapshot = new Dictionary<string, object>(dataDictionary);
            }
            return JsonConvert.SerializeObject(snapshot);
        }

        /// <summary>
        /// Clear the local data dictionary (workaround for missing data). Should be called when periodically
        /// </summary>
        public void ClearData()
        {
            lock (dataDictionary)
            {
                Console.WriteLine("Clearing data dictionary");
                dataDictionary.Clear();
                Console.WriteLine(JsonConvert.SerializeObject(dataDictionary));
            }
        }
    }
}
