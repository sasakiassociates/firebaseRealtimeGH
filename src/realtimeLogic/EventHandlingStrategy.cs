using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    internal interface EventHandlingStrategy
    {
    }

    internal class MarkerStrategy : EventHandlingStrategy
    {
        public void Update()
        {
            Console.WriteLine("Database updated");
        }
    }

    internal class ConfigStrategy : EventHandlingStrategy
    {
        public void Update()
        {
            Console.WriteLine("Database updated");
        }
    }
}
