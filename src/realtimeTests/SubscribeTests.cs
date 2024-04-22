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
        public async Task TearDown()
        {
            await repository.Unsubscribe();
        }

        [Test]
        public async Task SubscribeTest()
        {
            await repository.Subscribe(baseNode);
        }

        [Test]
        public async Task ReloadSubscriptionTest()
        {
            await repository.Subscribe(baseNode);

            await repository.Unsubscribe();

            await repository.Subscribe(baseNode);
        }
    }
}
