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
            baseNode = $"bases/{projectName}";
            credentials.SetSharedCredentials(keyPath, databaseURL, baseNode);

            repository = new Repository();
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public async Task BasicSendTest()
        {
            string destination = "test";
            string testJson = "test";
            await repository.PutAsync(destination, testJson);
        }
    }
}
