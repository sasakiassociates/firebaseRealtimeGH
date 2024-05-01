using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class Repository
    {
        private Credentials _credentials;                                           // Globally shared credentials for the Firebase database
        public ChildQuery baseQuery;                                                // Instance of the client to communicate with Firebase 
        DatabaseObserver observer;

        public bool authorized = false;                                             // Whether the user is authorized to access the database
        public bool subscribed = false;                                             // Whether the user is subscribed to the database

        public int flushInterval = 10000;                                           // Interval to flush the data

        public Repository()
        {
            _credentials = Credentials.GetInstance();
            _credentials.CredentialsChanged += OnCredentialsChanged;
            if (_credentials.baseChildQuery != null)
            {
                baseQuery = _credentials.baseChildQuery;
                authorized = true;
            }
        }

        public async Task SetTargetNode(string targetNode)
        {
            Action<string> callback = observer.callback;

            await observer.UnsubscribeAsync();
            observer = new DatabaseObserver(baseQuery, targetNode);
            await observer.Subscribe(callback);
        }

        /// <summary>
        /// Makes a subscription to the database and listens for updates to the target folder. You can set callback functions on this class to run when the data is updated.
        /// </summary>
        /// <param name="targetNodes"></param>
        /// <returns></returns>
        /// TODO for now, we'll limit the target nodes to a single node (multiple nodes might have caused race conditions)
        public async Task Subscribe(string targetNode, Action<string> callback)
        {
            // Initial Pull
            Dictionary<string, object> dataDict = await baseQuery.Child(targetNode).OnceSingleAsync<Dictionary<string, object>>();
            string data = JsonConvert.SerializeObject(dataDict);
            callback(data);

            observer = new DatabaseObserver(baseQuery, targetNode);
            await observer.Subscribe(callback);
            subscribed = true;

            // Start a thread to flush the data periodically
            _ = Task.Run(async () => { await FlushThread(); });
        }
        public async Task Subscribe(string targetNode, Action<string> callback, CancellationToken cancellationToken)
        {
            // Initial Pull
            string data = await baseQuery.Child(targetNode).OnceSingleAsync<string>();
            callback(data);

            observer = new DatabaseObserver(baseQuery, targetNode);
            await observer.Subscribe(callback);
            subscribed = true;
            // TODO add a cancellation token to the observer

            // Start a thread to flush the data periodically
            _ = Task.Run(async () => { await FlushThread(); });
        }

        /// <summary>
        /// Unsubscribes from the database. If there is no subscription, this does nothing.
        /// </summary>
        /// <returns></returns>
        public async Task UnsubscribeAsync()
        {
            if (subscribed) 
            {
                await observer.UnsubscribeAsync();
            }
            else
            {
                Console.WriteLine("No subscription to unsubscribe from");
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
                // Resubscribe to the shared credentials
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
        public async Task DeleteNode(string node)
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
        public void OnCredentialsChanged()
        {
            Action<string> action = null;
            string folder = "";

            if (subscribed)
            {
                action = observer.callback;
                folder = observer.folderName;
                Task.Run(async () => { await UnsubscribeAsync(); }).Wait();
            }

            baseQuery = _credentials.baseChildQuery;
            if (baseQuery != null)
            {
                authorized = true;
            }

            if (subscribed)
            {
                Task.Run(async () => { await Subscribe(folder ,action); }).Wait();
            }
        }

        /// <summary>
        /// Flush the local current data (workaround for bugs when the pull misses data)
        /// </summary>
        public async Task FlushThread()
        {
            while (subscribed)
            {
                // Wait the flush interval
                await Task.Delay(flushInterval);

                observer.ClearData();

                Console.WriteLine("Flushed data");
            }
        }

        /// <summary>
        /// Sets the time period in milliseconds to periodically flush the local data
        /// </summary>
        /// <param name="interval"></param>
        public void SetFlushInterval(int interval)
        {
            flushInterval = interval;
        }
    }
}
