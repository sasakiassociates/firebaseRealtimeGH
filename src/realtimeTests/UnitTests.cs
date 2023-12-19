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

            Console.WriteLine("----------------------------");

            // The second time this is called, it will return nothing since there's no new data until the database is updated
            markers = _repository.WaitForNewData(cancellationToken);

            _repository.Unsubscribe();

            Assert.Pass();
        }

        [Test]
        public void SubscribeForTenSecondsThenCancel()
        {
            _repository.Subscribe();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // The first time this is called, it will return all the data in the database since it's all new to the program
            //List<Marker> markers = _repository.WaitForNewData(cancellationToken);

            // Start a new thread where the cancellation token will be cancelled after 5 seconds
            Task.Run(() =>
            {
                Thread.Sleep(10000);
                cancellationTokenSource.Cancel();
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                // This should stop after 10 seconds, otherwise the cancellation token didn't work
                List<Marker> markers = _repository.WaitForNewData(cancellationToken);
            }

            _repository.Unsubscribe();

            Assert.Pass();
        }

    }
}