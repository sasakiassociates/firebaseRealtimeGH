using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace realtimeLogic
{
    /// <summary>
    /// Repository class is in charge of managing the connection to the Firebase database and the data flow (is composed of DatabaseObserver objects watching individual nodes). It tries to connect upon instantiation using the shared credentials, and if they are not available, it waits for them to be provided.
    /// </summary>
    public class Repository
    {
        private List<DatabaseObserver> databaseObservers = new List<DatabaseObserver>();
        public AutoResetEvent updateEvent = new AutoResetEvent(false);
        private AutoResetEvent reloadEvent = new AutoResetEvent(false);
        public bool connected = false;
        private Credentials credentials;
        public CancellationToken CancellationToken { get; set; }

        // TEMP delete the data locally every now and then as a workaround to the ghosting issue
        private int reloadInterval = 30000;
        private Task reloadThread;

        // Current connection data
        public FirebaseClient firebaseClient;
        public List<string> targetNodes = new List<string>();

        public Repository()
        {
            credentials = Credentials.GetInstance();
            // Subscribe to know when the shared credentials change
            credentials.CredentialsChanged += OnChangedSharedConnection;
            if (credentials.firebaseClient != null)
            {
                Console.WriteLine("Credentials already set");
                firebaseClient = credentials.firebaseClient;
                connected = true;
            }
        }

        ///////////////////////////////////////////////////////////////// SUBSCRIBING ///////////////////////////////////////////////////////////////////////////

        private async void ReloadConnection(CancellationToken cancellationToken)
        {
            while (connected && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(reloadInterval);
                ReloadConnection();
            }
        }

        /// <summary>
        /// Runs when local credential information is provided
        /// </summary>
        /// <param name="_pathToKeyFile"></param>
        /// <param name="_firebaseUrl"></param>
        public void OverrideLocalConnection(string _pathToKeyFile, string _firebaseUrl)
        {
            // If the inputs are null and we aren't subscribed to the shared credentials, resubscribe
            if (_pathToKeyFile == null && _firebaseUrl == null)
            {
                if (firebaseClient == null)
                {
                    return;
                }
                ResubscribeToSharedCredentials();
                return;
            }
            else
            {
                // Unsubscribe from the shared credentials
                credentials.CredentialsChanged -= OnChangedSharedConnection;

                // Make a local instance of the firebase client
                firebaseClient = new FirebaseClient(_firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(_pathToKeyFile), AsAccessToken = true });
            }
            // Reload the connection
            ReloadConnection();
        }

        /// <summary>
        /// Resubscribe to the shared credentials (should be run when the local credentials were set but are no longer needed)
        /// </summary>
        public void ResubscribeToSharedCredentials()
        {
            // Subscribe to know when the shared credentials change
            credentials.CredentialsChanged += OnChangedSharedConnection;
        }

        /// <summary>
        /// The function that gets called whenever the shared credentials change (see Credentials class)
        /// </summary>
        public void OnChangedSharedConnection()
        {
            Console.WriteLine("Credentials changed");

            // If the shared credentials are null, don't do anything
            if (credentials.firebaseClient == null)
            {
                return;
            }

            // If they're the same instance, don't do anything
            if (firebaseClient == credentials.firebaseClient)
            {
                return;
            }

            firebaseClient = credentials.firebaseClient;

            ReloadConnection();
        }

        /// <summary>
        /// Completely reloads the connection to the Firebase database
        /// </summary>
        public async void ReloadConnection()
        {

            await ReloadTargetNodeConnections();

            // Signal that the connection has been established, signalling all the threads running WaitForConnection to perform their action
            reloadEvent.Set();
        }

        /// <summary>
        /// Run this to clean up the connection
        /// </summary>
        public void Teardown()
        {
            // Unsubscribe observers
            foreach (DatabaseObserver observer in databaseObservers)
            {
                observer.Unsubscribe();
            }

            if (firebaseClient == null)
            {
                return;
            }
            firebaseClient.Dispose();
            firebaseClient = null;
            connected = false;
        }

        /// <summary>
        /// Sets the target nodes for the repository to observe (creates an observer object for each target node)
        /// </summary>
        /// <param name="_targetNodes"></param>
        public void SetTargetNodes(List<string> _targetNodes)
        {
            targetNodes = _targetNodes;

            if (connected)
            {
                _ = ReloadTargetNodeConnections();
            }
        }

        /// <summary>
        /// Reloads the connection to the target nodes by unsubscribing from the current observers and creating new ones
        /// </summary>
        /// <returns></returns>
        public async Task ReloadTargetNodeConnections()
        {
            foreach (DatabaseObserver observer in databaseObservers)
            {
                observer.Unsubscribe();
            }

            databaseObservers.Clear();

            if (reloadThread == null)
            {
                // Start the reload thread that will reload the connection every reloadInterval milliseconds (to avoid ghosting)
                reloadThread = Task.Run(() => ReloadConnection(CancellationToken));
            }

            if (targetNodes.Count == 0)
            {
                DatabaseObserver observer = new DatabaseObserver(firebaseClient.Child("test"));
                await observer.Subscribe(updateEvent);
                databaseObservers.Add(observer);
            }
            else
            {
                foreach (string folder in targetNodes)
                {
                    DatabaseObserver observer = new DatabaseObserver(firebaseClient.Child(folder));
                    await observer.Subscribe(updateEvent);
                    databaseObservers.Add(observer);
                }
            }

        }

        // TODO figure out what happens if we reload the connection while waiting for an update
        /// <summary>
        /// Wait for an update to happen in one of the target nodes or their children
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public string WaitForUpdate(CancellationToken cancellationToken)
        {
            WaitHandle.WaitAny(new WaitHandle[] { updateEvent, cancellationToken.WaitHandle, reloadEvent });

            string incomingData = "{";
            foreach (DatabaseObserver observer in databaseObservers)
            {
                if (observer.updatedData == null)
                {
                    continue;
                }
                incomingData += "\"" + observer.folderName + "\": " + observer.updatedData;
                // Check if this needs a comma
                incomingData += ",\n";
            }
            incomingData += "}";

            return incomingData;
        }

        // TODO figure out what happens if we reload the connection while waiting for an update
        /// <summary>
        /// This function subscribes the thread to wait until the Repository is connected to the Firebase database, then runs the action
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="action"></param>
        public void WaitForConnection(CancellationToken cancellationToken, Action action)
        {
            // If we're already connected, run the action
            if (connected)
            {
                action();
                return;
            }

            // Wait for the connection to be established or for the cancellation token to be triggered
            WaitHandle.WaitAny(new WaitHandle[] { reloadEvent, cancellationToken.WaitHandle });

            // If the connection is established, run the action; else go back to waiting
            if (connected)
            {
                action();
            }
            else
            {
                WaitForConnection(cancellationToken, action);
            }
        }

        ///////////////////////////////////////////////////////////////// SENDING ///////////////////////////////////////////////////////////////////////////

        // TODO whenever the updated datapoint matches the previous, it creates a new key in the database, but we want it to override
        /// <summary>
        /// Updates the data in the target node using a list of data points
        /// </summary>
        /// <param name="dataPoints"></param>
        /// <param name="targetNodeToUpdate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task PutAsync(List<object> dataPoints, string targetNodeToUpdate)
        {
            if (targetNodeToUpdate == null)
            {
                throw new Exception("Target folder is null");
                //await firebaseClient.Child("").PutAsync(json);
            }
            if (firebaseClient == null)
            {
                throw new Exception("Firebase client is null");
            }

            ChildQuery targetFolder = StringToChildQuery(targetNodeToUpdate);

            Console.WriteLine(JsonConvert.SerializeObject(dataPoints));

            await targetFolder.PutAsync(dataPoints);
        }
        /// <summary>
        /// Updates the data in the target node using a single data point
        /// </summary>
        /// <param name="dataPoint"></param>
        /// <param name="targetNodeToUpdate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task PutAsync(object dataPoint, string targetNodeToUpdate)
        {
            if (targetNodeToUpdate == null)
            {
                throw new Exception("Target folder is null");
                //await firebaseClient.Child("").PutAsync(json);
            }

            ChildQuery targetFolder = StringToChildQuery(targetNodeToUpdate);

            await targetFolder.PutAsync(dataPoint);
        }

        /// <summary>
        /// Deletes the target node
        /// </summary>
        /// <param name="targetNodeToDelete"></param>
        /// <exception cref="Exception"></exception>
        public void Delete(string targetNodeToDelete)
        {
            if (targetNodeToDelete == null)
            {
                throw new Exception("Target folder is null");
            }
            // split the target folder by the slashes
            string[] folders = targetNodeToDelete.Split('/');
            // The last string is the key
            string key = folders[folders.Length - 1];

            // Get rid of the key from the folders list
            Array.Resize(ref folders, folders.Length - 1);

            ChildQuery targetFolder = null;

            foreach (string folder in folders)
            {
                if (targetFolder != null)
                {
                    targetFolder = targetFolder.Child(folder);
                }
                else
                {
                    targetFolder = firebaseClient.Child(folder);
                }
            }

            targetFolder.Child(key).DeleteAsync();
        }

        /// <summary>
        /// Posts a Json string to the target node
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public string PushToProject(string folder, string json)
        {
            return firebaseClient.Child(folder).PostAsync(json).Result.Key;
        }

        /// <summary>
        /// Gets the access token for the Firebase database
        /// </summary>
        /// <param name="pathToKeyFile"></param>
        /// <returns></returns>
        private async Task<string> GetAccessToken(string pathToKeyFile)
        {
            var credential = GoogleCredential.FromFile(pathToKeyFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

        /// <summary>
        /// Pulls the data from the target node once
        /// </summary>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        public string PullOnce(string targetNode)
        {
            ChildQuery target = StringToChildQuery(targetNode);

            var data = target.OnceAsync<JToken>();

            string response = JsonConvert.SerializeObject(data.Result);
            return response;
        }

        /// <summary>
        /// Takes in the string of the desired folder and returns a ChildQuery object pointing to that folder on Firebase
        /// </summary>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private ChildQuery StringToChildQuery(string targetNode)
        {
            string[] nodes = targetNode.Split('/');
            ChildQuery target = null;
            foreach (string node in nodes)
            {
                   if (target != null)
                {
                    target = target.Child(node);
                }
                else
                {
                    target = firebaseClient.Child(node);    
                }
            }
            return target;
        }
    }
}
