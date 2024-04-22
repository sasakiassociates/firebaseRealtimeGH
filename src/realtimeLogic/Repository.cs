using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class Repository
    {
        private Credentials _credentials;                                           // Globally shared credentials for the Firebase database
        public ChildQuery baseQuery;                                                // Instance of the client to communicate with Firebase 

        List<RepoObserver> observers = new List<RepoObserver>();

        IDisposable subscription;                                                   // Subscription to the database
        public bool authorized = false;                                             // Whether the user is authorized to access the database
        public ChildQuery observingFolder;                                                 // The folder in the database that we are observing
        string subscriptionId;
        private Dictionary<string, string> databaseDictionary = new Dictionary<string, string>();
        public string updatedData;
        private Debouncer debouncer = Debouncer.GetInstance();
        public Action callback { get; set; }
        string targetNode;


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
        public async Task Subscribe(string targetNode)
        {
            this.targetNode = targetNode;

            subscriptionId = Guid.NewGuid().ToString();
            observingFolder = baseQuery.Child(targetNode);

            await observingFolder.Child("listener").PutAsync($"{{\"{subscriptionId}\": {{\"status\" : \"listening\"}}}}");

            // Subscribe to the database with the given callback called whenever an update event is triggered
            subscription = observingFolder.AsObservable<string>().Subscribe(_firebaseEvent =>
            {
                Console.WriteLine($"Received event: {_firebaseEvent.EventType} {targetNode} {_firebaseEvent.Key} {_firebaseEvent.Object}");
                // Use the key (e.g., object ID) to identify each object
                string objectId = _firebaseEvent.Key;
                string data = _firebaseEvent.Object;

                // TODO find a better way to handle these cases
                if (_firebaseEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                {
                    databaseDictionary[objectId] = data;
                }
                else if (_firebaseEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
                {
                    databaseDictionary.Remove(objectId);
                }

                debouncer.Debounce(() =>
                {
                    updatedData = DatabaseToString();
                    if (callback != null)
                    {
                        callback();
                    }
                });
            });
            Console.WriteLine($"Subscribed to {targetNode}");
        }

        public async Task ReloadSubscription()
        {
            await Unsubscribe();
            await Subscribe(targetNode);
        }

        private string DatabaseToString()
        {
            if (databaseDictionary.Count == 0)
            {
                return null;
            }

            string output = "{\n";
            // TODO this enumeration gets interrupted by new data coming in, so it's not thread safe
            foreach (var key in databaseDictionary.Keys)
            {
                output += $" \"{key}\": {databaseDictionary[key]},\n";
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
        /// Unsubscribes from the database. If there is no subscription, this does nothing.
        /// </summary>
        /// <returns></returns>
        public async Task Unsubscribe()
        {
            if (subscription == null)
            {
                Console.WriteLine("No subscription to unsubscribe from");
                return;
            }
            subscription?.Dispose();
            await observingFolder.Child($"listener/{subscriptionId}").DeleteAsync();
            Console.WriteLine("Unsubscribed from the database");
        }

        /// <summary>
        /// Override the shared connections on this Repository with the given path to the key file and Firebase URL
        /// </summary>
        /// <param name="_pathToKeyFile"></param>
        /// <param name="_firebaseUrl"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public async Task OverrideLocalConnection(string _pathToKeyFile, string _firebaseUrl, string basePath = "")
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

            await ReloadSubscription();
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
        public async Task PutAsync(object data, string destination)
        {
            await baseQuery.Child(destination).PutAsync(data);
        }

        /// <summary>
        /// Does a one time pull of data from the database at the specified destination
        /// </summary>
        /// <param name="destination"></param>
        public void PullData(string destination)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs whenever the Credentials class fires the CredentialsChanged event. We need to set the baseQuery to the new credentials and recreate the observers if we have any.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void OnCredentialsChanged()
        {
            baseQuery = _credentials.baseChildQuery;
        }
    }
}
