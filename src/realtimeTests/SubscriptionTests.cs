using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using realtimeLogic;
using Newtonsoft.Json.Linq;
using Firebase.Database.Query;
using Firebase.Database;
using Google.Apis.Auth.OAuth2;
using static Google.Apis.Requests.BatchRequest;

namespace realtimeTests
{
    public class SubscriptionTests
    {
        Logger logger = Logger.GetInstance();
        Repository _repo;
        FirebaseConnectionManager _creds;
        Dictionary<string, object> nodeSentMessage = new Dictionary<string, object>();
        string baseName;
        string testBaseNode;

        [SetUp]
        public async Task Setup()
        {
            string keyPath = "L:\\sa_strategies\\TableUI\\key\\firebase_table-key.json";
            string databaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
            _creds = FirebaseConnectionManager.GetInstance();
            _creds.SetSharedCredentials(keyPath, databaseUrl);

            baseName = "TestBase";
            testBaseNode = $"bases/{baseName}";

            _repo = _creds.CreateRepository("test-repo");
            await _repo.Subscribe(testBaseNode);
        }

        [TearDown]
        public async Task TearDown()
        {
            foreach (var item in nodeSentMessage)
            {
                await _repo.DeleteAsync(item.Key);
            }
            await _repo.UnsubscribeAsync();

            string logs = logger.ReadLog();
            Console.WriteLine(logs);
        }

        [Test]
        public void SubscribeTest()
        {
            Assert.That(_repo.isSubscribed);
        }

        [Test]
        public async Task BasicSendTest()
        {
            string destination = "test";
            JObject sendingObject = new JObject();
            sendingObject["test"] = "test";

            await _repo.PutAsync(destination, sendingObject);
            nodeSentMessage.Add(destination, sendingObject);
            Console.WriteLine("Sent test message");

            var response = await _repo.PullAsync(destination);

            if (response == null)
            {
                Assert.Fail();
            }

            JObject responseObject = JObject.Parse(response.ToString());

            Console.WriteLine("Comparing " + sendingObject.ToString() + " to " + responseObject.ToString());
            if (JToken.DeepEquals(sendingObject, responseObject))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public async Task StringTest()
        {
            bool recievedString = false;
            JObject sendingObject = new JObject();
            sendingObject["test"] = "test";

            EventHandler<DictChangedEventArgs> handler = (sender, e) =>
            {
                foreach(var item in e.UpdatedDict)
                {
                    JObject responseObject = JObject.Parse(item.ToString());
                    if (JToken.DeepEquals(responseObject, sendingObject))
                    {
                        recievedString = true;
                    }
                }
            };

            string destination = "test";
            await _repo.PutAsync(destination, sendingObject);
            nodeSentMessage.Add(destination, sendingObject);

            Assert.IsTrue(recievedString);
        }

        public void TestCallback()
        {
            
        }
    }
}
