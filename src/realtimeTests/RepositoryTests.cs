using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using realtimeLogic;

namespace realtimeTests
{
    public class RepositoryTests
    {
        Repository repository;
        Credentials credentials;

        [SetUp]
        public async Task Setup()
        {
            string databaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
            string keyPath = "C:\\Users\\nshikada\\Documents\\GitHub\\table\\key\\firebase_table-key.json";
            string projectName = "debug_proj";

            credentials = Credentials.GetInstance();
            credentials.SetSharedCredentials(keyPath, databaseUrl, projectName);

            repository = new Repository(projectName);
            await repository.Subscribe();
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
            await repository.DeleteNodeAsync(destination);
        }

        [Test]
        public async Task LogEventsTest()
        {
        }

        [Test]
        public async Task ListChangedTest()
        {

        }
    }
}
