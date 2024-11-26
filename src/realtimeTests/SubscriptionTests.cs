using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using realtimeLogic;
using Newtonsoft.Json.Linq;

namespace realtimeTests
{
    public class SubscriptionTests
    {
        Repository _repo;
        Credentials _creds;

        [SetUp]
        public async Task Setup()
        {
            string databaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
            string keyPath = "C:\\Users\\nshikada\\Documents\\GitHub\\table\\key\\firebase_table-key.json";
            string projectName = "test_base";

            _creds = Credentials.GetInstance();
            _creds.SetSharedCredentials(keyPath, databaseUrl, $"bases/{projectName}");

            _repo = new Repository("TestRepository");
            await _repo.Subscribe();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _repo.UnsubscribeAsync();
        }

        [Test]
        public async Task BasicSendTest()
        {
            string destination = "test";
            string testJson = "{{\"text\": \"test\"}}";

            await _repo.PutAsync(destination, testJson);

            Task.Delay(1000).Wait();
        }

        [Test]
        public async Task StringTest()
        {
            bool recievedString = false;
            string testJson = "{{\"text\": \"test\"}}";

            EventHandler<DictChangedEventArgs> handler = (sender, e) =>
            {
                foreach(var item in e.UpdatedDict)
                {
                    if (item.Value.ToString() == testJson)
                    {
                        recievedString = true;
                    }
                }
            };

            string destination = "test";
            await _repo.PutAsync(destination, testJson);

            Assert.IsTrue(recievedString);
        }

        public void TestCallback()
        {
            
        }
    }
}
