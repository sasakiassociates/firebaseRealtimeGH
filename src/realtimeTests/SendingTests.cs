using realtimeLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace realtimeTests
{
    public class SendingTests
    {
        string projectName = "test_proj";
        string baseNode;
        string databaseURL = "https://magpietable-default-rtdb.firebaseio.com/";
        string keyPath = "C:\\Users\\nshikada\\Documents\\GitHub\\table\\key\\firebase_table-key.json";
        Repository repository;

        [SetUp]
        public void Setup()
        {
            Credentials credentials = Credentials.GetInstance();
            baseNode = $"bases/{projectName}/marker";
            credentials.SetSharedCredentials(keyPath, databaseURL, baseNode);

            repository = new Repository();
        }

        [TearDown]
        public async Task TearDown()
        {
            await repository.UnsubscribeAsync();
        }

        [Test]
        public async Task BasicSendTest()
        {
            string destination = "test";
            string testJson = "test";
            await repository.PutAsync(destination, testJson);
        }

        [Test]
        public async Task SubscribeTest()
        {
            await repository.Subscribe("", async (data) =>
            {
                Console.WriteLine(data);
            });

            // Sleep for 10 seconds to allow the subscription to run
            await Task.Delay(10000);
        }
    }
}
