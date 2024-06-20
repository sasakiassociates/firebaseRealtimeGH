using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class Repository
    {
        private Credentials _credentials;                                           // Globally shared credentials for the Firebase database
        public ChildQuery baseQuery;                                                // Instance of the client to communicate with Firebase 

        IDisposable subscription;
        IDisposable deletionSubscription;
        
        // The name we'll put under the listener node when we subscribe
        public string _name;

        public bool authorized = false;                                             // Whether the user is authorized to access the database
        public bool subscribed = false;                                             // Whether the user is subscribed to the database
        Debouncer debouncer = new Debouncer();                                      // Debouncer to prevent multiple updates from firing

        private ChildQuery observingNode;                                           // The node this repository is observing

        private Dictionary<string, object> _currentItems;

        // Event handlers for notifying changes
        public event EventHandler<ListChangedEventArgs> ListChanged;
        public event EventHandler<LogEventArgs> LogEvent;

        public Repository(string name, string targetNode = "")
        {
            _name = name;
            _credentials = Credentials.GetInstance();
            _credentials.CredentialsChanged += OnCredentialsChanged;                // Subscribe to the credentials changed event
            if (_credentials.baseChildQuery != null)
            {
                baseQuery = _credentials.baseChildQuery;
                authorized = true;
            }
            _currentItems = new Dictionary<string, object>();
            SetTargetNode(targetNode);
        }

        /// <summary>
        /// Sets the target node for this repository. Should be called before subscribing to the database.
        /// </summary>
        /// <param name="targetNode"></param>
        public async void SetTargetNode(string targetNode)
        {
            observingNode = baseQuery.Child(targetNode);
            if (!authorized)
            {
                Log("Not authorized to access the database");
                return;
            }
            if (subscribed)
            {
                await UnsubscribeAsync();
                await Subscribe();
            }
        }

        /// <summary>
        /// Makes a subscription to the database and listens for updates to the target folder. You can set callback functions on this class to run when the data is updated.
        /// </summary>
        /// <returns></returns>
        public async Task Subscribe()
        {
            if (!authorized)
            {
                Log("Not authorized to access the database");
                return;
            }

            // Initial Pull
            _currentItems = await observingNode.OnceSingleAsync<Dictionary<string, object>>();
            if (_currentItems == null)
            {
                _currentItems = new Dictionary<string, object>();
            }
            else
            {
                ListChanged?.Invoke(this, new ListChangedEventArgs(_currentItems));
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string time = DateTime.Now.ToString("HH:mm:ss");
            // Put a placeholder in the listeners folder to indicate that this observer is listening (subscribe only works when there is already data in the folder)
            await observingNode.Child($"listeners/{_name}").PutAsync($"{{\"Subscribed at\": \"{date}|{time}\"}}");

            try
            {
                // Listen for new items added to the database
                subscription = observingNode.AsObservable<JToken>().Where(f => f.EventType == FirebaseEventType.InsertOrUpdate)
                    .Subscribe(f => HandleItemAddedOrUpdated(f.Key, f.Object));
                // Listen for items removed from the database
                deletionSubscription = observingNode.AsObservable<JToken>().Where(f => f.EventType == FirebaseEventType.Delete)
                    .Subscribe(f => HandleItemDeleted(f.Key, f.Object));

                Log("Subscribed");
                subscribed = true;
            }
            catch (Exception e)
            {
                Log($"Error subscribing: {e.Message}");
            }
        }

        /// <summary>
        /// Called when an object in the subscribed node is updated
        /// </summary>
        private void HandleItemAddedOrUpdated(string key, JToken item)
        {
            if (item == null)
            {
                Log($"Item {key} is null");
                return;
            }

            // This is a special case for the update_interval node so we can control the frequency of updates
            if (key == "update_interval")
            {
                // Convert the data to an int
                int milliseconds = int.Parse(item.ToString());
                debouncer.SetDebounceDelay(milliseconds);
                return;
            }

            // check the is_deleted field to see if we should delete the item
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
                Log($"Item added: {key}");
            }
            OnListChanged();
        }

        /// <summary>
        /// Called when an object in the subscribed node is deleted
        /// </summary>
        private void HandleItemDeleted(string key, JToken item)
        {
            if (item == null)
            {
                Log($"Item {key} is null");
                return;
            }

            if (_currentItems.ContainsKey(key))
            {
                _currentItems.Remove(key);
                OnListChanged();
                Log($"Item removed: {key}");
            }
        }

        /// <summary>
        /// Will call the ListChanged event and debounce the callback function
        /// </summary>
        protected virtual void OnListChanged()
        {
            // After the debounce period, call the ListChanged event
            debouncer.Debounce(() => ListChanged?.Invoke(this, new ListChangedEventArgs(_currentItems)));
        }

        /// <summary>
        /// Will log the given message to listeners to the LogEvent
        /// </summary>
        /// <param name="message"></param>
        protected virtual void Log(string message)
        {
            LogEvent?.Invoke(this, new LogEventArgs(message));
        }

        /// <summary>
        /// Unsubscribes from the database. If there is no subscription, this does nothing.
        /// </summary>
        /// <returns></returns>
        public async Task UnsubscribeAsync()
        {
            if (subscribed) 
            {
                await observingNode.Child($"listeners/{_name}").DeleteAsync();
                subscription.Dispose();
                deletionSubscription.Dispose();
                Log("Unsubscribed");
            }
            else
            {
                Log("No subscription to unsubscribe from");
            }

            subscribed = false;
        }

        /// <summary>
        /// Override the shared connections on this Repository with the given path to the key file and Firebase URL
        /// </summary>
        /// <param name="_pathToKeyFile"></param>
        /// <param name="_firebaseUrl"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public void OverrideLocalConnection(string _pathToKeyFile, string _firebaseUrl, string basePath = "")
        {
            authorized = false;
            if (_pathToKeyFile == null && _firebaseUrl == null)
            {
                // Resubscribe to the shared credentials if no local credentials are provided
                _credentials.CredentialsChanged += OnCredentialsChanged;

                if (_credentials.baseChildQuery != null)
                {
                    baseQuery = _credentials.baseChildQuery;
                    authorized = true;
                }
                return;
            }
            else
            {
                // Unsubscribe from the shared credentials
                _credentials.CredentialsChanged -= OnCredentialsChanged;

                FirebaseClient newClient = new FirebaseClient(_firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => Credentials.GetAccessToken(_pathToKeyFile), AsAccessToken = true });
                baseQuery = newClient.Child(basePath);
                authorized = true;
            }

        }

        /// <summary>
        /// Deletes the specified node from the database
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public async Task DeleteNodeAsync(string node)
        {
            await baseQuery.Child(node).DeleteAsync();
        }

        /// <summary>
        /// Puts the given data into the specified destination in the database
        /// </summary>
        /// <param name="data"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        /*public async Task PutAsync(string destination, List<object> dataPoints)
        {
            await baseQuery.Child(destination).PutAsync(dataPoints);
        }*/
        public async Task PutAsync(string destination, object data)
        {
            await baseQuery.Child(destination).PutAsync(data);
        }

        /// <summary>
        /// Does a one time pull of data from the database at the specified destination
        /// </summary>
        /// <param name="destination"></param>
        public async Task<object> PullData(string destination)
        {
            var response = await baseQuery.Child(destination).OnceAsJsonAsync();
            return response;
        }

        /// <summary>
        /// Runs whenever the Credentials class fires the CredentialsChanged event. We need to set the baseQuery to the new credentials and recreate the observers if we have any.
        /// </summary>
        public async void OnCredentialsChanged()
        {
            if (subscribed)
            {
                await UnsubscribeAsync();
            }

            baseQuery = _credentials.baseChildQuery;
            if (baseQuery != null)
            {
                authorized = true;
            }

            if (subscribed)
            {
                await Subscribe();
            }
        }

        /// <summary>
        /// Reload the subscription to the database
        /// </summary>
        public async void Reload()
        {
            await UnsubscribeAsync();
            await Subscribe();
        }
    }
}
