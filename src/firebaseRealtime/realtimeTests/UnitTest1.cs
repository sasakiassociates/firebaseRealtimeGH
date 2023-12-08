using realtimeLogic;

namespace realtimeTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            Repository.Instance.Test();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}