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
        string project;

        private Repository(string pathToKeyFile, string firebaseUrl, string projectName)
        {
            firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            project = projectName;
        }

        public static Repository GetInstance(string pathToKeyFile, string firebaseUrl, string projectName)
        {
            if (instance == null)
            {
                lock (typeof(Repository))
                {
                    if (instance == null)
                    {
                        instance = new Repository(pathToKeyFile, firebaseUrl, projectName);
                    }
                }
            }
            return instance;
        }

        public async Task Setup(List<string> foldersToObserve)
        {
            foreach (string folder in foldersToObserve)
            {
                DatabaseObserver observer =  new DatabaseObserver(firebaseClient, folder, project);
                await observer.Subscribe(updateEvent);
                databaseObservers.Add(observer);
            }
        }

        public string PushToProject(string folder, string json)
        {
            return firebaseClient.Child("bases").Child(project).Child(folder).PostAsync(json).Result.Key;
        }

        public string WaitForUpdate(CancellationToken cancellationToken)
        {
            //updateEvent.WaitOne();
            WaitHandle.WaitAny(new WaitHandle[] { updateEvent, cancellationToken.WaitHandle });

            string markerData = DataManager.GetInstance().markerData.ToString();
            Console.WriteLine(markerData);
            return markerData;
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
