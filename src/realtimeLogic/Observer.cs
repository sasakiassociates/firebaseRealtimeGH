using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public interface IObserver
    {
    }

    public class DatabaseObserver : IObserver
    {
        public void Update()
        {
            Console.WriteLine("Database updated");
        }
    }
}
