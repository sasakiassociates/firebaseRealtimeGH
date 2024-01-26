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

        public string sharedDatabaseUrl;
        public string sharedKeyDirectory;

        public void SetSharedCredentials(string _pathToKeyFile, string _firebaseUrl)
        {
            sharedKeyDirectory = _pathToKeyFile;
            sharedDatabaseUrl = _firebaseUrl;

            CredentialsChanged?.Invoke();
        }
    }
}
