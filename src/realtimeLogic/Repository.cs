using Firebase.Database;
using Firebase.Database.Query;
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

        /// <summary>
        /// Makes a subscription to the database and listens for updates to the target folder. You can set callback functions on this class to run when the data is updated.
        /// </summary>
        /// <param name="targetNodes"></param>
        /// <returns></returns>
        /// TODO for now, we'll limit the target nodes to a single node (multiple nodes might have caused race conditions)
        public async Task Subscribe(string targetNode, Action<string> callback)
        {
            observer = new DatabaseObserver(baseQuery, targetNode);
            await observer.Subscribe(callback);
            subscribed = true;
        }
        public async Task Subscribe(string targetNode, Action<string> callback, CancellationToken cancellationToken)
        {
            observer = new DatabaseObserver(baseQuery, targetNode);
            await observer.Subscribe(callback);
            subscribed = true;
            // TODO add a cancellation token to the observer
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
        public async Task PutAsync(string destination, List<object> dataPoints)
        {
            await baseQuery.Child(destination).PutAsync(dataPoints);
        }

        /// <summary>
        /// Does a one time pull of data from the database at the specified destination
        /// </summary>
        /// <param name="destination"></param>
        public async Task PullData(string destination)
        {
            await baseQuery.Child(destination).OnceSingleAsync<string>();
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
            authorized = true;
            
            if (subscribed)
            {
                Task.Run(async () => { await Subscribe(folder ,action); }).Wait();
            }
        }
    }
}
