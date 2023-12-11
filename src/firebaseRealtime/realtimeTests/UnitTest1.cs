using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        Repository _repository;

        [SetUp]
        public void Setup()
        {
            _repository = Repository.Instance;
        }

        [Test]
        public void Test1()
        {
             _repository.TestRetrieve();
            
            Assert.Pass();
        }

        [Test]
        public void Test2()
        {
            _repository.TestSubscribe();
            
            Assert.Pass();
        }
    }
}