using Firebase.Database.Query;
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
        List<Marker>? markers;

        Repository repository;

        [SetUp]
        public void Setup()
        {
            Credentials credentials = Credentials.GetInstance();
            baseNode = $"bases/{projectName}";
            credentials.SetSharedCredentials(keyPath, databaseURL, baseNode);

            repository = new Repository("test", "marker");

            EventHandler<LogEventArgs> logEventHandler = (sender, e) =>
            {
                Console.WriteLine(e.Message);
            };
            repository.LogEvent += logEventHandler;

            Debouncer debouncer = new Debouncer();
            debouncer.SetDebounceDelay(300);
            markers = new List<Marker>();
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
            await repository.Subscribe();

            Assert.That(repository.subscribed, Is.True);
        }

        [Test]
        public async Task ReloadSubscriptionTest()
        {
        }

        [Test]
        public async Task CallbackTest()
        {
        }

        [Test]
        public async Task LogEventObserverTest()
        {
            EventHandler<LogEventArgs> logEventHandler = (sender, e) =>
            {
                Console.WriteLine(e.Message);
            };

            repository.LogEvent += logEventHandler;

            await repository.Subscribe();

            await Task.Delay(1000);

            repository.LogEvent -= logEventHandler;

            await repository.UnsubscribeAsync();

            Assert.Pass();
        }

        [Test]
        public async Task ListChangedEventObserverTest()
        {
            EventHandler<ListChangedEventArgs> listChangedEventHandler = (sender, e) =>
            {
                foreach (var item in e.UpdatedList)
                {
                    Marker marker;
                    try
                    {
                        // ! is a null-forgiving operator
                        marker = Newtonsoft.Json.JsonConvert.DeserializeObject<Marker>(item.Value.ToString()!) ?? new Marker();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            };

            repository.ListChanged += listChangedEventHandler;

            await repository.Subscribe();

            await Task.Delay(1000);

            await repository.PutAsync($"/marker/childTest", new List<object> { "{{\"test\": \"test\"}}" });

            await Task.Delay(5000);

            await repository.DeleteNodeAsync($"/marker/childTest");

            repository.ListChanged -= listChangedEventHandler;

            await repository.UnsubscribeAsync();

            Assert.Pass();
        }

        [Test]
        public async Task IsDeletedTest()
        {
            EventHandler<ListChangedEventArgs> listChangedEventHandler = (sender, e) =>
            {
                foreach (var item in e.UpdatedList)
                {
                    Marker marker;
                    try
                    {
                        // ! is a null-forgiving operator
                        marker = Newtonsoft.Json.JsonConvert.DeserializeObject<Marker>(item.Value.ToString()!) ?? new Marker();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            };

            Marker deletedMarker = new Marker { x = 10, y = 20, rotation = 45, is_deleted = true };
            Marker marker = new Marker { x = 10, y = 20, rotation = 45, is_deleted = false };
            repository.ListChanged += listChangedEventHandler;

            await repository.Subscribe();

            await repository.PutAsync($"marker/deletedMarker", deletedMarker);

            await Task.Delay(1000);

            await repository.PutAsync($"marker/marker", marker);

            await Task.Delay(1000);

            marker.is_deleted = true;

            await repository.PutAsync($"marker/marker", marker);

            await Task.Delay(1000);

            await repository.DeleteNodeAsync($"marker/deletedMarker");
            await repository.DeleteNodeAsync($"marker/marker");

            Assert.Pass();
        }

    }

    public class Marker
    {
        public string uuid { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public float rotation { get; set; }
        public bool is_deleted { get; set; }
    }
}
