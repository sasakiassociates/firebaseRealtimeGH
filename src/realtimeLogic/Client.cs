using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public class Client
    {
        List<DatabaseObserver> databaseObservers = new List<DatabaseObserver>();
        Repository repository;

        public Client(string pathToKeyFile, string firebaseUrl)
        {
            repository = Repository.GetInstance(pathToKeyFile, firebaseUrl);
        }
    }
}
