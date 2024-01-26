using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NUnit.Framework.Internal.Execution;
using realtimeLogic;
using System.Diagnostics.Contracts;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;

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
        Repository repository;

        string testListenerString = " \"listener\": {\"status\": \"listening\"}";
        string testMarkerChangeString = " \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -700, \"y\": -500, \"rotation\": 0}";
        string testMarkerChangeString2 = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -650, \"y\": -450, \"rotation\": 0.5}}";
        string testMarkerChangeString3 = "{ \"1e537e37-54c0-4c64-8751-da51a6e1abf4\": { \"id\": 5, \"x\": -600, \"y\": -400, \"rotation\": 1}}";

        [SetUp]
        public async Task Setup()
        {
            Credentials.GetInstance().SetSharedCredentials(pathToKeyFile, firebaseUrl);
            repository = new Repository();
            //repository.OverrideLocalConnection(pathToKeyFile, firebaseUrl);
        }

        [TearDown]
        public async Task TearDown()
        {
            repository.Teardown();
        }

        [Test]
        public async Task TestKeyFile()
        {
            FirebaseClient _firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            var data = _firebaseClient.Child("").OnceAsync<JToken>();

            string response = Newtonsoft.Json.JsonConvert.SerializeObject(data.Result);

            Console.WriteLine(response);

            Assert.Pass();
        }

        [Test]
        public async Task SubscribeUnsubscribe()
        {
            await Task.Run(() => Thread.Sleep(100));

            Assert.That(repository.connected, Is.True);
        }

        [Test]
        public async Task PullOnce()
        {
            string markers = repository.PullOnce("");
            Console.WriteLine(markers);

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
                string markers = repository.WaitForUpdate(cancellationToken);
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

            await repository.PutAsync(points, "bases/test/config/cad_points");
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
            await repository.PutAsync(testCadPoints, "bases/test_proj/config/cad_points");
            Console.WriteLine("Put");

            Assert.Pass();
        }

        [Test]
        public async Task SetGlobalConfig()
        {
            List<int> bounding_markers = new List<int> { 0, 1, 2, 3 };

            await repository.PutAsync(bounding_markers, "global_config/bounding_markers");
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

            await repository.PutAsync(tableData, "tables");
            Assert.Pass();
        }

        [Test]
        public async Task SubscribePostToConfigReceieve()
        {
            repository.SetTargetNodes(new List<string> { "bases/test_base/config" });

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
                await repository.PutAsync(testDict, "bases/test_base/config");
                Thread.Sleep(1000);
                await repository.PutAsync(newTest, "bases/test_base/config");
                Thread.Sleep(1000);
                repository.Delete("bases/test_base/config");
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for update");
                // This should stop after 10 seconds, otherwise the cancellation token didn't work
                string message = repository.WaitForUpdate(cancellationToken);
            }

            Assert.Pass();
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

        [Test]
        public async Task SubscribeUpdate()
        {
            FirebaseClient _firebaseClient = new FirebaseClient(firebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(pathToKeyFile), AsAccessToken = true });
            ChildQuery observingFolder = _firebaseClient.Child("bases").Child("test_base").Child("config");

            await observingFolder.PutAsync("{\"test\": 1}");

            int updates = 0;
            IDisposable subscription = observingFolder
                .AsObservable<JToken>()
                .Subscribe(_firebaseEvent =>
                {
                    if (_firebaseEvent.Key == null || _firebaseEvent.Key == "")
                    {
                        Console.WriteLine("no key");
                        return;
                    }

                    Console.WriteLine($"Received event: {_firebaseEvent.EventType} {_firebaseEvent.Key} {_firebaseEvent.Object}");
                    updates++;
                    Console.WriteLine(updates);
                });

            _ = Task.Run(async () =>
            {
                Dictionary<string, object> testDict = new Dictionary<string, object>
                {
                    { "bounding_markers", new List<int> { 0, 1, 2, 3 } }
                };
                Thread.Sleep(1000);
                Dictionary<string, object> newTest = new Dictionary<string, object>
                {
                    { "bounding_markers", new List<int> { 0, 1, 2, 3, 4 } }
                };
                await observingFolder.PostAsync(testDict);
                Thread.Sleep(1000);
                await observingFolder.PostAsync(newTest);
                Thread.Sleep(1000);
                repository.Delete("bases/test_base/config");
            });

            Thread.Sleep(5000);

            subscription.Dispose();
        }

        private async Task<string> GetAccessToken(string pathToKeyFile)
        {
            var credential = GoogleCredential.FromFile(pathToKeyFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }
    }
}