using realtimeLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace realtimeTests
{
    public class SubscribeTests
    {
        string projectName = "test_proj";
        string databaseURL = "https://magpietable-default-rtdb.firebaseio.com/";
        string keyPath = "C:\\Users\\nshikada\\Documents\\GitHub\\table\\key\\firebase_table-key.json";

        Repository repository;

        [SetUp]
        public void Setup()
        {
            Credentials credentials = Credentials.GetInstance();
            credentials.SetSharedCredentials(keyPath, databaseURL, projectName);

            repository = new Repository();
        }

        [TearDown]
        public void TearDown()
        {
            repository.Teardown();
        }

        [Test]
        public async Task SubscribeTest()
        {
            
        }
    }
}
