using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Streaming;
using Google.Apis.Auth.OAuth2;

namespace realtimeLogic
{
    internal class Repository
    {
        internal readonly FirebaseClient firebaseClient;
        private static Repository instance;
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
