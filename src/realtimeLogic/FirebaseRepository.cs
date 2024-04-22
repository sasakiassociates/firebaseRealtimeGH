using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public class FirebaseRepository
    {
        private List<DatabaseObserver> _observers = new List<DatabaseObserver>();   // List of observers that are listening to the database
        private Credentials _credentials;                                           // Globally shared credentials for the Firebase database
        public bool authenticated = false;                                          // Whether the repository is connected to the database
        public FirebaseClient firebaseClient;                                       // Instance of the client to communicate with Firebase 

        public FirebaseRepository()
        {
            _credentials = Credentials.GetInstance();
            _credentials.CredentialsChanged += OnCredentialsChanged;
            if (_credentials.firebaseClient != null)
            {
                firebaseClient = _credentials.firebaseClient;
                authenticated = true;
            }
        }

        public async Task Subscribe(Action callback)
        {
            throw new NotImplementedException();
        }

        public async Task ReloadSubscription()
        {
            throw new NotImplementedException();
        }

        public async Task Unsubscribe()
        {
            throw new NotImplementedException();
        }

        public void SendData()
        {
            throw new NotImplementedException();
        }

        public void PullData()
        {
            throw new NotImplementedException();
        }

        public void OnCredentialsChanged()
        {
            throw new NotImplementedException();
        }
    }
}
