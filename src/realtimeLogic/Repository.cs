using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Google.Apis.Auth.OAuth2;

namespace realtimeLogic
{
    public class Repository
    {
        internal readonly FirebaseClient firebaseClient;
        private static Repository instance;
        private List<DatabaseObserver> databaseObservers = new List<DatabaseObserver>();
        public AutoResetEvent updateEvent = new AutoResetEvent(false);

        private Repository(string pathToKeyFile, string firebaseUrl)
        {
            firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
        }

        public static Repository GetInstance(string pathToKeyFile, string firebaseUrl)
        {
            if (instance == null)
            {
                lock (typeof(Repository))
                {
                    if (instance == null)
                    {
                        instance = new Repository(pathToKeyFile, firebaseUrl);
                    }
                }
            }
            return instance;
        }

        public async Task PutAsync(string json)
        {
            await firebaseClient.Child("bases").Child("test_proj").Child("config").PutAsync(json);
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

            string incomingData = "";
            foreach (DatabaseObserver observer in databaseObservers)
            {
                if (observer.updatedData != null)
                { 
                    incomingData += observer.folderName + ": " + observer.updatedData;
                }
            }

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
