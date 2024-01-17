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
            _repository = Repository.GetInstance();
            _repository.Connect(pathToKeyFile, firebaseUrl);
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

        // Sending this way makes sure that the previous data is overriden
        [Test]
        public async Task PutAsyncObject()
        {
            List < object > points = new List<object>();
            /*points.Add(new int[] { -543256, -206823 });
            points.Add(new int[] { 119459, 134001 });
            points.Add(new int[] { 319944, -255831 });
            points.Add(new int[] { -342771, -596656 });*/
            points.Add(new int[] { -789, -206823 });
            points.Add(new int[] { 6542, 3456 });
            points.Add(new int[] { 596656, -119364459 });
            points.Add(new int[] { -363, -596656 });

            await _repository.PutAsync(points, "bases/test_proj/config/cad_points");
        }

        // Seinding this way appends new data to the existing data if it isn't already there
        [Test]
        public async Task PutAsyncList()
        {
            //string testJson = "{\"listening\": true}";
            List<object> testCadPoints = new List<object>
            {
                new object[] { -543256.559,-206823.998 },
                new object[] { 119459.325,134001.313 },
                new object[] { 319944.803, -255831.560 },
                new object[] { -342771.081, -596656.872 },
            };
            /*List<object> testObjects = new List<object>();
            testObjects.Add(new { id = 1, x = 0, y = 0, rotation = 0 });
            testObjects.Add(new { id = 2, x = 100, y = 100, rotation = 0 });*/

            await _repository.PutAsync(testCadPoints, "bases/test_proj/config/cad_points");
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