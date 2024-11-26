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

            repository = new Repository("test_repository");
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
            string destination = "flags/test";
            string testJson = "test";
            await repository.PutAsync(destination, testJson);
            await repository.DeleteNodeAsync(destination);
        }

        #region Subscribe Tests

        /// <summary>
        /// Tests the ability of subscribe to recieve updates to string data
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task SubscribeStringTest()
        {
            // Subscribe to the test node
            string destination = "test";
            string testJson = "text";
            await repository.PutAsync(destination, testJson);
        }

        [Test]
        public async Task SubscribeIntTest()
        {

        }

        [Test]
        public async Task SubscribeDoubleTest()
        {

        }

        [Test]
        public async Task SubscribeBoolTest()
        {

        }

        [Test]
        public async Task SubscribeObjectTest()
        {

        }

        #endregion

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
