using Firebase.Database;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class Credentials
    {
        public string database_url;
        public string key_directory;
        private static Credentials instance;
        public List<Repository> observingRepositories = new List<Repository>();
        public FirebaseClient firebaseClient;

        private Credentials()
        {
        }

        public static Credentials GetInstance()
        {
            if (instance == null)
            {
                lock (typeof(Credentials))
                {
                    if (instance == null)
                    {
                        instance = new Credentials();
                    }
                }
            }
            return instance;
        }

        public void AddRepository(Repository repository)
        {
            foreach (Repository r in observingRepositories)
            {
                if (r == repository)
                {
                    return;
                }
            }
            // If firebase client is connected, give the repository my credentials
            if (firebaseClient != null)
            {
                repository.Register(firebaseClient);
            }
            observingRepositories.Add(repository);
        }
        public void RemoveRepository(Repository repository)
        {
            observingRepositories.Remove(repository);
        }

        private void SetCredentials()
        {
           foreach (Repository repository in observingRepositories)
            {
                repository.Register(firebaseClient);
            }
        }

        public void Connect(string pathToKeyFile, string firebaseUrl)
        {
            firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            SetCredentials();
        }

        public void Disconnect()
        {
            firebaseClient.Dispose();
            firebaseClient = null;
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
