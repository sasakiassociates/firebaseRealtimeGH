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
    public class Repository
    {
        public FirebaseClient firebaseClient;
        private List<DatabaseObserver> databaseObservers = new List<DatabaseObserver>();
        public AutoResetEvent updateEvent = new AutoResetEvent(false);
        private AutoResetEvent reloadEvent = new AutoResetEvent(false);
        public bool connected = false;
        private Credentials credentials = Credentials.GetInstance();

        // Current connection data
        public string keyDirectory = "";
        public string url = "";
        public List<string> targetNodes = new List<string>();

        public Repository()
        {
            // Subscribe to know when the shared credentials change
            credentials.CredentialsChanged += OnChangedSharedConnection;
            if (credentials.sharedDatabaseUrl != null)
            {
                keyDirectory = credentials.sharedKeyDirectory;
                url = credentials.sharedDatabaseUrl;
                ReloadConnection();
            }
        }

        public void OverrideLocalConnection(string _pathToKeyFile, string _firebaseUrl)
        {
            // Unsubscribe from the shared credentials
            credentials.CredentialsChanged -= OnChangedSharedConnection;

            keyDirectory = _pathToKeyFile;
            url = _firebaseUrl;

            ReloadConnection();
        }

        public void ResubscribeToSharedCredentials()
        {
            // Subscribe to know when the shared credentials change
            credentials.CredentialsChanged += OnChangedSharedConnection;
        }

        public void OnChangedSharedConnection()
        {
            Console.WriteLine("Credentials changed");

            keyDirectory = credentials.sharedKeyDirectory;
            url = credentials.sharedDatabaseUrl;

            ReloadConnection();
        }

        public async void ReloadConnection()
        {
            if (connected) { Teardown(); }

            try
            {
                Console.WriteLine($"Connecting to Firebase at {url} using key {keyDirectory}");
                firebaseClient = new FirebaseClient(url, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(keyDirectory), AsAccessToken = true });
                connected = true;

                if (targetNodes.Count > 0)
                {
                    await ReloadTargetNodeConnections();
                }

                reloadEvent.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Teardown()
        {
            // Unsubscribe observers
            foreach (DatabaseObserver observer in databaseObservers)
            {
                observer.Unsubscribe();
            }

            firebaseClient.Dispose();
            firebaseClient = null;
            connected = false;
        }

        public void SetTargetNodes(List<string> _targetNodes)
        {
            if (targetNodes == _targetNodes)
            {
                return;
            }
            targetNodes = _targetNodes;

            if (connected)
            {
                _ = ReloadTargetNodeConnections();
            }
        }

        public async Task ReloadTargetNodeConnections()
        {
            foreach (DatabaseObserver observer in databaseObservers)
            {
                observer.Unsubscribe();
            }

            databaseObservers.Clear();

            foreach (string folder in targetNodes)
            {
                DatabaseObserver observer = new DatabaseObserver(firebaseClient, folder);
                await observer.Subscribe(updateEvent);
                databaseObservers.Add(observer);
            }
        }

        // TODO figure out what happens if we reload the connection while waiting for an update
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

        // TODO whenever the updated datapoint matches the previous, it creates a new key in the database, but we want it to override
        public async Task PutAsync(List<object> dataPoints, string _targetFolderString)
        {
            if (_targetFolderString == null)
            {
                throw new Exception("Target folder is null");
                //await firebaseClient.Child("").PutAsync(json);
            }
            if (firebaseClient == null)
            {
                throw new Exception("Firebase client is null");
            }
            // split the target folder by the slashes
            string[] folders = _targetFolderString.Split('/');

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

            Console.WriteLine(JsonConvert.SerializeObject(dataPoints));

            await targetFolder.PutAsync(dataPoints);
        }
        public async Task PutAsync(object dataPoint, string pathToObjectToUpdate)
        {
            if (pathToObjectToUpdate == null)
            {
                throw new Exception("Target folder is null");
                //await firebaseClient.Child("").PutAsync(json);
            }
            // split the target folder by the slashes
            string[] folders = pathToObjectToUpdate.Split('/');

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

            await targetFolder.PutAsync(dataPoint);
        }

        public void Delete(string pathToObjectToUpdate)
        {
            if (pathToObjectToUpdate == null)
            {
                throw new Exception("Target folder is null");
            }
            // split the target folder by the slashes
            string[] folders = pathToObjectToUpdate.Split('/');
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


        public string PushToProject(string folder, string json)
        {
            return firebaseClient.Child(folder).PostAsync(json).Result.Key;
        }

        private async Task<string> GetAccessToken(string pathToKeyFile)
        {
            var credential = GoogleCredential.FromFile(pathToKeyFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

        public string PullOnce(string targetNode)
        {
            ChildQuery target = StringToChildQuery(targetNode);

            var data = target.OnceAsync<JToken>();

            string response = JsonConvert.SerializeObject(data.Result);
            return response;
        }

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
