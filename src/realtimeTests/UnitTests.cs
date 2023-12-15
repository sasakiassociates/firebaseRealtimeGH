using Microsoft.VisualBasic;
using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        Repository<Marker> _repository;
        string firebaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
        string pathToKeyFile = @"C:\Users\nshikada\Documents\GitHub\firebaseRealtimeGH\keys\firebase_table-key.json";

        [SetUp]
        public void Setup()
        {
            _repository = Repository<Marker>.GetInstance(pathToKeyFile, firebaseUrl);
        }

        [Test]
        public async Task RetrieveFromDatabaseAsync()
        {
            await _repository.RetrieveAsync();

            Console.WriteLine(_repository.parsedObjectList.Count);
            
            Assert.Pass();
        }

        [Test]
        public void SubscribeUnsubscribe()
        {
            _repository.Subscribe();
            
            Thread.Sleep(1000);

            _repository.Unsubscribe();

            Assert.Pass();
        }

        [Test]
        public void SubscribeRetrieveUnsubscribe()
        {
            _repository.Subscribe();
            
            Thread.Sleep(1000);

            _ = _repository.RetrieveAsync();

            Thread.Sleep(1000);

            _repository.Unsubscribe();

            Assert.Pass();
        }

        [Test]
        public void WaitForNewData()
        {
            _repository.Subscribe();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Thread.Sleep(1000);

            // The first time this is called, it will return all the data in the database since it's all new to the program
            List<Marker> markers = _repository.WaitForNewData(cancellationToken);

            foreach (Marker marker in markers)
            {
                Console.WriteLine(marker.uuid);
            }

            // The second time this is called, it will return nothing since there's no new data until the database is updated
            markers = _repository.WaitForNewData(cancellationToken);

            foreach (Marker marker in markers)
            {
                Console.WriteLine(marker.uuid);
            }

            _repository.Unsubscribe();

            Assert.Pass();
        }

        [Test]
        public void CancelWaitForNewData()
        {
            _repository.Subscribe();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Thread.Sleep(1000);

            // The first time this is called, it will return all the data in the database since it's all new to the program
            List<Marker> markers = _repository.WaitForNewData(cancellationToken);

            foreach (Marker marker in markers)
            {
                Console.WriteLine(marker.uuid);
            }

            // Start a new thread where the cancellation token will be cancelled after 5 seconds
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                cancellationTokenSource.Cancel();
            });

            // This should stop after 5 seconds, otherwise the cancellation token didn't work
            _repository.WaitForNewData(cancellationToken);

            _repository.Unsubscribe();

            Assert.Pass();
        }

        public void SubscribeForFiveSeconds()
        {
            _repository.Subscribe();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            List<Marker> markers = new List<Marker>();

            while (true)
            {
                markers = _repository.WaitForNewData(cancellationToken);
            }

            _repository.Unsubscribe();
        }
    }
}