using Microsoft.VisualBasic;
using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        Repository<Marker> _repository;

        [SetUp]
        public void Setup()
        {
            _repository = Repository<Marker>.GetInstance;
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
    }
}