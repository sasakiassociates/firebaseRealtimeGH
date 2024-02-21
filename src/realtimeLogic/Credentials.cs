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
        private static Credentials instance;
        public event Action CredentialsChanged;

        public string sharedDatabaseUrl;
        public string sharedKeyDirectory;

        private Credentials()
        {
            sharedKeyDirectory = null;
            sharedDatabaseUrl = null;
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

        public void SetSharedCredentials(string _pathToKeyFile, string _firebaseUrl)
        {
            sharedKeyDirectory = _pathToKeyFile;
            sharedDatabaseUrl = _firebaseUrl;

            CredentialsChanged?.Invoke();
        }

        /// <summary>
        /// Erase the credentials from the shared variables
        /// </summary>
        public void EraseCredentials()
        {
            sharedKeyDirectory = null;
            sharedDatabaseUrl = null;
        }
    }
}
