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
        string baseNode;
        string databaseURL = "https://magpietable-default-rtdb.firebaseio.com/";
        string keyPath = "C:\\Users\\nshikada\\Documents\\GitHub\\table\\key\\firebase_table-key.json";

        Repository repository;

        [SetUp]
        public void Setup()
        {
            Credentials credentials = Credentials.GetInstance();
            credentials.SetSharedCredentials(keyPath, databaseURL, baseNode);
            baseNode = $"bases/{projectName}";

            repository = new Repository();
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await repository.UnsubscribeAsync();

            Thread.Sleep(1000);
        }

        [Test]
        public async Task SubscribeTest()
        {
            await repository.Subscribe(baseNode, async (data) =>
            {
                Console.WriteLine(data);
            });

            await Task.Delay(10000);
        }

        [Test]
        public async Task ReloadSubscriptionTest()
        {
            await repository.Subscribe(baseNode, async (data) =>
            {
                Console.WriteLine(data);
            });

            await repository.UnsubscribeAsync();

            await repository.Subscribe(baseNode, async (data) =>
            {
                Console.WriteLine(data);
            });
        }

        [Test]
        public async Task CallbackTest()
        {
            await repository.Subscribe($"{baseNode}/test", async (data) =>
            {
                Console.WriteLine(data);
                Console.WriteLine("Testing Callback");
            });

            await repository.PutAsync($"{baseNode}/test/childTest", new List<object> { "{{\"test\": \"test\"}}" });

            await Task.Delay(1000);

            await repository.DeleteNode($"{baseNode}/test/childTest");

            await Task.Delay(1000);
        }

        [Test]
        public async Task ReloadTest()
        {
            await repository.Subscribe($"{baseNode}/test", async (data) =>
            {
                Console.WriteLine(data);
            });

            await Task.Delay(3000);

            await repository.UnsubscribeAsync();
            await repository.Subscribe($"{baseNode}/test2", async (data) =>
            {
                Console.WriteLine(data);
            });

            await Task.Delay(3000);
        }

    }
}
