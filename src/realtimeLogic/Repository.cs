using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;

namespace realtimeLogic
{
    public class Repository
    {
        internal FirebaseClient firebaseClient;
        private static Repository instance;
        private List<DatabaseObserver> databaseObservers = new List<DatabaseObserver>();
        public AutoResetEvent updateEvent = new AutoResetEvent(false);
        public bool connected = false;

        private Repository()
        {
        }

        public static Repository GetInstance()
        {
            if (instance == null)
            {
                lock (typeof(Repository))
                {
                    if (instance == null)
                    {
                        instance = new Repository();
                    }
                }
            }
            return instance;
        }

        public void Connect(string pathToKeyFile, string firebaseUrl)
        {
            firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            if (firebaseClient != null)
            {
                connected = true;
            }
        }

        // TODO whenever the updated datapoint matches the previous, it creates a new key in the database, but we want it to override
        public async Task PutAsync(List<object> dataPoints, string _targetFolderString)
        {
            if (_targetFolderString == null)
            {
                throw new Exception("Target folder is null");
                //await firebaseClient.Child("").PutAsync(json);
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

            //Console.WriteLine(JsonConvert.SerializeObject(dataPoint));

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

        public ChildQuery SetTargetFolder(string _targetFolder)
        {
            ChildQuery targetFolder = null;
            // Split the target folder into parent and child
            string[] targetFolderSplit = _targetFolder.Split('/');
            foreach (string folder in targetFolderSplit)
            {
                targetFolder = firebaseClient.Child(folder);
            }
            return targetFolder;
        }

        public async Task Setup(List<string> _targetFolders)
        {
            foreach (string folder in _targetFolders)
            {
                DatabaseObserver observer = new DatabaseObserver(firebaseClient, folder);
                await observer.Subscribe(updateEvent);
                databaseObservers.Add(observer);
            }
        }

        public string PushToProject(string folder, string json)
        {
            return firebaseClient.Child(folder).PostAsync(json).Result.Key;
        }

        public string WaitForUpdate(CancellationToken cancellationToken)
        {
            WaitHandle.WaitAny(new WaitHandle[] { updateEvent, cancellationToken.WaitHandle });

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

        public async Task Teardown()
        {
            // Unsubscribe observers
            foreach (DatabaseObserver observer in databaseObservers)
            {
                await observer.Unsubscribe();
            }
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
    }
}
