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
        Logger _logger = Logger.GetInstance();

        private static Credentials instance;
        // TODO make this private
        public FirebaseClient firebaseClient;

        public event Action CredentialsChanged;
        public ChildQuery baseChildQuery;
        public bool isAuthorized = false;

        private Credentials()
        {
            Log("Initialized");
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
                // Check if the _pathToKeyFile exists
                if (!System.IO.File.Exists(_pathToKeyFile))
                {
                    Log("The key file does not exist");
                    return;
                }

                try
                {
                    firebaseClient = new FirebaseClient(_firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(_pathToKeyFile), AsAccessToken = true });

                    baseChildQuery = firebaseClient.Child(basePath);

                    isAuthorized = true;
                } catch (Exception e)
                {
                    Log("Error setting credentials: " + e.Message);
                    return;
                }

                Log("Credentials set");

                CredentialsChanged?.Invoke();
            }
            catch (Exception e)
            {
                Log("Error setting credentials: " + e.Message);
            }
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
            Log("Credentials erased");
        }

        /// <summary>
        /// Will log the given message to listeners to the LogEvent
        /// </summary>
        /// <param name="message"></param>
        protected virtual void Log(string message)
        {
            _logger.Log(this, message);
        }
    }
}
