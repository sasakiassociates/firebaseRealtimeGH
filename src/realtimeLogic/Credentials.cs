using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class Credentials
    {
        private static Credentials instance;
        // TODO make this private
        public FirebaseClient firebaseClient;

        public event Action CredentialsChanged;
        public ChildQuery baseChildQuery;
        public bool isAuthorized = false;

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

        public void SetSharedCredentials(string _pathToKeyFile, string _firebaseUrl, string basePath = "")
        {
            try
            {
                firebaseClient = new FirebaseClient(_firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(_pathToKeyFile), AsAccessToken = true });

                baseChildQuery = firebaseClient.Child(basePath);

                isAuthorized = true;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            CredentialsChanged?.Invoke();
        }

        /// <summary>
        /// Gets the access token for the Firebase database
        /// </summary>
        /// <param name="pathToKeyFile"></param>
        /// <returns></returns>
        public static async Task<string> GetAccessToken(string pathToKeyFile)
        {
            var credential = GoogleCredential.FromFile(pathToKeyFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

        /// <summary>
        /// Erase the credentials from the shared variables
        /// </summary>
        public void EraseCredentials()
        {
            firebaseClient = null;
            baseChildQuery = null;
            isAuthorized = false;
        }
    }
}
