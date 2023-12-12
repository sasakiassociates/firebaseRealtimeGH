using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        Repository<Marker> _repository;

        [SetUp]
        public void Setup()
        {
            _repository = Repository<Marker>.Instance;
        }

        [Test]
        public void RetrieveFromDatabase()
        {
             _repository.Retrieve();
            
            Assert.Pass();
        }

        [Test]
        public void SubscribeUnsubscribe()
        {
            _repository.Subscribe();
            
            Thread.Sleep(1000);

            _repository.EndSubscription();

            Assert.Pass();
        }

        [Test]
        public void SubscribeRetrieveUnsubscribe()
        {
            _repository.Subscribe();
            
            Thread.Sleep(1000);

            _repository.Retrieve();

            Thread.Sleep(1000);

            _repository.EndSubscription();

            Assert.Pass();
        }
    }
}