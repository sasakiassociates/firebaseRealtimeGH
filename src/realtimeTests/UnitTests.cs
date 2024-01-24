using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NUnit.Framework.Internal.Execution;
using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        private const string testConfigJson = " \"config\": " +
            "{\r\n  \"base_aruco_marker_id\": 8," +
            "\r\n  \"cad_points\": [\r\n    " +
            "{\r\n      \"Boundingbox\": " +
            "{\r\n        \"Area\": 0.0,\r\n        \"Center\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 100.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"Diagonal\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 0.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"IsValid\": true," +
            "\r\n        \"Max\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 100.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"Min\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 100.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"Volume\": 0.0\r\n      }," +
            "\r\n      \"ClippingBox\": " +
            "{\r\n        \"Area\": 0.0," +
            "\r\n        \"Center\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 100.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"Diagonal\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 0.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"IsValid\": true," +
            "\r\n        \"Max\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 100.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"Min\": " +
            "{\r\n          \"X\": 0.0,\r\n          \"Y\": 100.0,\r\n          \"Z\": 0.0\r\n        }," +
            "\r\n        \"Volume\": 0.0\r\n      }," +
            "\r\n      \"IsGeometryLoaded\": true," +
            "\r\n      \"IsReferencedGeometry\": false," +
            "\r\n      \"IsValid\": true," +
            "\r\n      \"IsValidWhyNot\": \"\"," +
            "\r\n      \"QC_Type\": 5," +
            "\r\n      \"ReferenceID\": \"00000000-0000-0000-0000-000000000000\"," +
            "\r\n      \"TypeDescription\": \"3D Point coordinate\"," +
            "\r\n      \"TypeName\": \"Point\"," +
            "\r\n      \"Value\": " +
            "{\r\n        \"X\": 0.0,\r\n        \"Y\": 100.0,\r\n        \"Z\": 0.0\r\n      }\r\n    }]," +
            "\r\n  \"update_interval\": 1000\r\n}\r\n";
        string firebaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
        string pathToKeyFile = @"C:\Users\nshikada\Documents\GitHub\firebaseRealtimeGH\keys\firebase_table-key.json";
        Repository _repository;

        string testListenerString = " \"listener\": {\"status\": \"listening\"}";
        string testMarkerChangeString = " \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -700, \"y\": -500, \"rotation\": 0}";
        string testMarkerChangeString2 = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -650, \"y\": -450, \"rotation\": 0.5}}";
        string testMarkerChangeString3 = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -600, \"y\": -400, \"rotation\": 1}}";

        [SetUp]
        public async Task Setup()
        {
            _repository = Repository.GetInstance();
            _repository.Connect(pathToKeyFile, firebaseUrl);
            List<string> foldersToWatch = new List<string> { "bases/test_proj/marker", "bases/test_proj/config" };
            //await _repository.Setup(foldersToWatch);
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

        // Sending this way makes sure that the previous data is overriden
        [Test]
        public async Task PutAsyncObject()
        {
            List < object > points = new List<object>();
            points.Add(new int[] { 6542, 3456 });
            points.Add(new int[] { 596656, -119364459 });
            points.Add(new int[] { -363, -596656 });

            await _repository.PutAsync(points, "bases/test_proj/config/cad_points");
        }

        // Seinding this way appends new data to the existing data if it isn't already there
        [Test]
        public async Task PutAsyncList()
        {
            List<object> testCadPoints = new List<object>
            {
                new object[] { -543256.559,-206823.998 },
                new object[] { 119459.325,134001.313 },
                new object[] { 319944.803, -255831.560 },
                new object[] { -342771.081, -596656.872 },
            };
            await _repository.PutAsync(testCadPoints, "bases/test_proj/config/cad_points");
            Console.WriteLine("Put");

            Assert.Pass();
        }

        [Test]
        public async Task SetGlobalConfig()
        {
            List<int> bounding_markers = new List<int> { 0, 1, 2, 3 };

            await _repository.PutAsync(bounding_markers, "global_config/bounding_markers");
            Assert.Pass();
        }

        [Test]
        public async Task SetTableData()
        {
            string tableName = "noguchi";
            string location = "fabrication studio";

            Dictionary<string, object> tableData = new Dictionary<string, object>();

            tableData[tableName] = new Dictionary<string, object>
            {
                { "location", location }
            };

            await _repository.PutAsync(tableData, "tables");
            Assert.Pass();
        }

        [Test]
        public async Task SubscribePostToConfigReceieve()
        {
            await _repository.Setup(new List<string> { "bases/test_proj" });

            // Start a thread that waits for an update
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Start a new thread where the cancellation token will be cancelled after 5 seconds
            _ = Task.Run(() =>
            {
                Thread.Sleep(10000);
                cancellationTokenSource.Cancel();
            });

            _ = Task.Run(async () =>
            {
                Dictionary<string, object> testDict = new Dictionary<string, object>
                {
                    { "bounding_markers", new List<int> { 0, 1, 2, 3 } }
                };
                Thread.Sleep(3000);
                Dictionary<string, object> newTest = new Dictionary<string, object>
                {
                    { "bounding_markers", new List<int> { 0, 1, 2, 3, 4 } }
                };
                await _repository.PutAsync(testDict, "bases/test_proj/config/test");
                Thread.Sleep(1000);
                await _repository.PutAsync(newTest, "bases/test_proj/config/test");
                Thread.Sleep(1000);
                _repository.Delete("bases/test_proj/config/test");
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for update");
                // This should stop after 10 seconds, otherwise the cancellation token didn't work
                string message = _repository.WaitForUpdate(cancellationToken);
                Console.WriteLine(message);
            }
        }

        [Test]
        public void ParseConfigJson()
        {
            dynamic jsonObject = JsonConvert.DeserializeObject(testConfigJson);

            Console.WriteLine(jsonObject.config.base_aruco_marker_id);
            Console.WriteLine(jsonObject.config.cad_points[0].Value.X);
            Console.WriteLine(jsonObject.config.cad_points[0].Value.Y);
            Assert.Pass();
        }

        [Test]
        public void ParseCombinedJson()
        {
            string combinedJson = "{";
            combinedJson += testConfigJson;
            combinedJson += ",";
            combinedJson += testListenerString;
            combinedJson += ",";
            combinedJson += testMarkerChangeString;
            combinedJson += "}";

            dynamic jsonObject = JsonConvert.DeserializeObject(combinedJson);
            Console.WriteLine(jsonObject.listener.status);
            Console.WriteLine(jsonObject["1e537e37-54c0-4c64-8751-da51a6e1abf4"].id);
            Console.WriteLine(jsonObject["1e537e37-54c0-4c64-8751-da51a6e1abf4"].x);
            Console.WriteLine(jsonObject["1e537e37-54c0-4c64-8751-da51a6e1abf4"].y);
            Assert.Pass();
        }
    }
}