using Microsoft.VisualBasic;
using NUnit.Framework.Internal.Execution;
using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        string firebaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
        string pathToKeyFile = @"C:\Users\nshikada\Documents\GitHub\firebaseRealtimeGH\keys\firebase_table-key.json";
        Repository _repository;

        string testMarkerString = "{ \"listener\": {\"status\": \"listening\"}}";
        string testMarkerChangeString = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -700, \"y\": -500, \"rotation\": 0}}";
        string testMarkerChangeString2 = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -650, \"y\": -450, \"rotation\": 0.5}}";
        string testMarkerChangeString3 = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -600, \"y\": -400, \"rotation\": 1}}";

        [SetUp]
        public async Task Setup()
        {
            _repository = Repository.GetInstance(pathToKeyFile, firebaseUrl);
            List<string> foldersToWatch = new List<string> { "bases/test_proj/marker", "bases/test_proj/config" };
            await _repository.Setup(foldersToWatch);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _repository.Teardown();
        }

        [Test]
        public async Task SubscribeUnsubscribe()
        {
            await Task.Run(() => Thread.Sleep(100));

            Assert.Pass();
        }

        [Test]
        public async Task SubscribeForTenSecondsThenCancel()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Start a new thread where the cancellation token will be cancelled after 5 seconds
            _ = Task.Run(() =>
            {
                Thread.Sleep(6000);
                cancellationTokenSource.Cancel();
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                // This should stop after 10 seconds, otherwise the cancellation token didn't work
                string markers = _repository.WaitForUpdate(cancellationToken);
            }

            Assert.Pass();
        }

        /*[Test]
        public async Task PostAsync()
        {
            string testJson = "{\"listening\": true, \"Test\": \"Yes\"}";

            await _repository.PostAsync("marker", testJson);
            Console.WriteLine("Posted");

            Assert.Pass();
        }

        [Test]
        public async Task DeleteAsync()
        {
            string target = "update_interval";
            await _repository.DeleteAsync(target, "config");
            Assert.Pass();
        }

        [Test]
        public async Task PutConfigData()
        {
            string intervalJson = "{\"update_interval\": 1000}";
            await _repository.PutAsync(intervalJson, "config");
            Assert.Pass();
        }*/

        [Test]
        public async Task PutAsync()
        {
            //string testJson = "{\"listening\": true}";
            string testCadPoints = "{\"cad_points\": [[-509061.077,-150217.700], [120741.836,184999.979], [321393.888,-191982.664], [-308409.025,-527200.343]]}";

            await _repository.PutAsync(testCadPoints);
            Console.WriteLine("Put");

            Assert.Pass();
        }

        /*[Test]
        public async Task SubscribePostDeleteUnsubscribe()
        {
            await _repository.PutAsync(testMarkerString);
            Console.WriteLine("Posted");

            await _repository.SubscribeAsync();

            await _repository.PutAsync(testMarkerChangeString);
            await _repository.PutAsync(testMarkerChangeString2);
            await _repository.DeleteAsync("1e537e37-54c0-4c64-8751-da51a6e1abf4");
            await _repository.PutAsync(testMarkerChangeString3);

            // Sleep for a second
            await Task.Run(() => Thread.Sleep(1000));

            await _repository.DeleteAsync("1e537e37-54c0-4c64-8751-da51a6e1abf4");
            await _repository.DeleteAsync("listener");

            await _repository.UnsubscribeAsync();
            Console.WriteLine("Unsubscribed");

            Assert.Pass();
        }*/
    }
}