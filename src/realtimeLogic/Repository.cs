using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class Repository
    {
        Logger _logger;
        public event Action UpdatedCredentials;                                     // Reference this action to reload the parent component when the credentials change
        public ChildQuery _baseQuery;                                                // Instance of the client to communicate with Firebase 

        IDisposable subscription;
        
        // The name we'll put under the listener node when we subscribe
        public string _name;
        internal bool selfAuthorized = false;                                            // Whether the user is authorized to access the database

        public bool isSubscribed = false;                                             // Whether the user is subscribed to the database
        Debouncer debouncer = new Debouncer();                                      // Debouncer to prevent multiple updates from firing

        private ChildQuery observingNode;                                           // The node this repository is observing

        private Dictionary<string, object> _currentItems;

        // Event handlers for notifying changes
        public event EventHandler<DictChangedEventArgs> DictChanged;

        internal Repository(string name, ChildQuery baseQuery)
        {
            try
            {
                _logger = Logger.GetInstance();
                Log("Initialized");
                _name = name;
                _baseQuery = baseQuery;
                _currentItems = new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Log($"Error initializing repository: {e.Message}");
            }
        }

        /// <summary>
        /// Makes a subscription to the database and listens for updates to the target folder. You can set callback functions on this class to run when the data is updated.
        /// </summary>
        /// <returns></returns>
        public async Task SubscribeAsync(string targetNode = "")
        {
            try
            {
                observingNode = _baseQuery.Child(targetNode);

                string date = DateTime.Now.ToString("yyyy-MM-dd");
                string time = DateTime.Now.ToString("HH:mm:ss");
                // Put a placeholder in the listeners folder to indicate that this observer is listening (subscribe only works when there is already data in the folder)
                await observingNode.Child($"listeners/{_name}").PutAsync($"{{\"Subscribed at\": \"{date}|{time}\"}}");

                try
                {
                    // Listen for new items added to the database
                    subscription = observingNode.AsObservable<JToken>().Where(f => f.EventType == FirebaseEventType.InsertOrUpdate)
                        .Subscribe(f => HandleItemAddedOrUpdated(f.Key, f.Object));

                    Log("Subscribed");
                    isSubscribed = true;
                }
                catch (Exception e)
                {
                    Log($"Error subscribing: {e.Message}");
                }
            }
            catch (Exception e)
            {
                Log($"Error subscribing: {e.Message}");
            }
        }

        // Called when an object in the subscribed node is updated
        private void HandleItemAddedOrUpdated(string key, JToken item)
        {
            try
            {
                if (item == null)
                {
                    Log($"Tried to update null item: {key}");
                    return;
                }

                // This is a special case for the update_interval node so we can control the frequency of updates using a variable on the database
                if (key == "update_interval")
                {
                    // Convert the data to an int
                    int milliseconds = int.Parse(item.ToString());
                    debouncer.SetDebounceDelay(milliseconds);
                    return;
                }

                // ISSUE this might be the thing blocking the code when there are multiple updates in a single moment
                //if (item.Type == JTokenType.Object)
                //{
                // check the is_deleted field to see if we should delete the item from the local representation
                if (item["is_deleted"] != null && (bool)item["is_deleted"])
                {
                    HandleItemDeleted(key, item);
                    return;
                }

                if (_currentItems.ContainsKey(key))
                {
                    _currentItems[key] = item;
                }
                else
                {
                    _currentItems.Add(key, item);
                    //Log($"Item added: {key}");
                }
                //}
                OnDictChanged();
            }
            catch (Exception e)
            {
                Log($"Error handling item added or updated: {e.Message}");
            }
        }

        /// <summary>
        /// Called when an object in the subscribed node is deleted
        /// </summary>
        private void HandleItemDeleted(string key, JToken item)
        {
            try
            {
                if (item == null)
                {
                    Log($"Tried to delete null item: {key}");
                    return;
                }

                if (_currentItems.ContainsKey(key))
                {
                    _currentItems.Remove(key);
                    OnDictChanged();
                    //Log($"Item removed: {key}");
                }
            }
            catch (Exception e)
            {
                Log($"Error handling item deleted: {e.Message}");
            }
        }

        /// <summary>
        /// Will call the ListChanged event and debounce the callback function
        /// </summary>
        protected virtual void OnDictChanged()
        {
            try
            {
                // After the debounce period, call the ListChanged event
                debouncer.Debounce(() => DictChanged?.Invoke(this, new DictChangedEventArgs(_currentItems)));
            }
            catch (Exception e)
            {
                Log($"Error calling ListChanged event: {e.Message}");
            }
        }

        /// <summary>
        /// Will log the given message to listeners to the LogEvent
        /// </summary>
        /// <param name="message"></param>
        protected virtual void Log(string message)
        {
            _logger.Log(this, message);
        }

        /// <summary>
        /// Unsubscribes from the database. If there is no subscription, this does nothing.
        /// </summary>
        /// <returns></returns>
        public async Task UnsubscribeAsync()
        {
            try
            {
                if (isSubscribed) 
                {
                    await observingNode.Child($"listeners/{_name}").DeleteAsync();
                    subscription.Dispose();
                    Log("Unsubscribed");
                }
                else
                {
                    Log("No subscription to unsubscribe from");
                }

                isSubscribed = false;
            }
            catch (Exception e)
            {
                Log($"Error unsubscribing: {e.Message}");
            }
        }

        /// <summary>
        /// Override the shared connections on this Repository with the given path to the key file and Firebase URL
        /// </summary>
        /// <param name="_pathToKeyFile"></param>
        /// <param name="_firebaseUrl"></param>
        /// <param name="basePath"></param>
        /// <returns>
        /// 
        /// </returns>
        public bool OverrideLocalConnection(string _pathToKeyFile, string _firebaseUrl, string basePath = "")
        {
            try
            {
                if (_pathToKeyFile == null && _firebaseUrl == null)
                {
                    selfAuthorized = false;
                }
                else
                {
                    FirebaseClient newClient = new FirebaseClient(_firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => FirebaseConnectionManager.GetAccessToken(_pathToKeyFile), AsAccessToken = true });
                    _baseQuery = newClient.Child(basePath);
                    selfAuthorized = true;
                    UpdatedCredentials?.Invoke();
                }
            }
            catch
            {
                selfAuthorized = false;
                Log("Error overriding local connection");
            }
            return selfAuthorized;
        }

        public void UpdateCredentials(ChildQuery newQuery)
        {
            Task.Run(async () => await UnsubscribeAsync()).Wait();
            _baseQuery = newQuery;
            Task.Run(async () => await SubscribeAsync()).Wait();
        }

        /// <summary>
        /// Deletes the specified node from the database
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string node)
        {
            try
            {
                await _baseQuery.Child(node).DeleteAsync();
            }
            catch (Exception e)
            {
                Log($"Error deleting node: {e.Message}");
            }
        }

        /// <summary>
        /// Puts the given data into the specified destination in the database
        /// </summary>
        /// <param name="data"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public async Task PutAsync(string destination, object data)
        {
            try
            {
                await _baseQuery.Child(destination).PutAsync(data);
            }
            catch (Exception e)
            {
                Log($"Error putting data: {e.Message}");
            }
        }

        /// <summary>
        /// Does a one time pull of data from the database at the specified destination
        /// </summary>
        /// <param name="destination"></param>
        public async Task<object> PullAsync(string destination)
        {
            try
            {
                var response = await _baseQuery.Child(destination).OnceAsJsonAsync();
                return response;
            }
            catch (Exception e)
            {
                Log($"Error pulling data: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reload the subscription to the database
        /// </summary>
        public async Task Reload()
        {
            try
            {
                await UnsubscribeAsync();
                await SubscribeAsync();
            } catch (Exception e)
            {
                Log($"Error reloading {_name} repository: {e.Message}");
            }
        }
    }
}
