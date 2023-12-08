using System;

namespace realtimeLogic
{
    public class Repository
    {
        public static readonly Repository Instance = new Repository();

        private Repository()
        {
            Console.WriteLine("Repository");
        }

        public void Test()
        {
            Console.WriteLine("Test");
        }

    }
}
