using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public class DictChangedEventArgs: EventArgs
    {
        public Dictionary<string, object> UpdatedDict { get; }

        public DictChangedEventArgs(Dictionary<string, object> updatedDict)
        {
            UpdatedDict = updatedDict;
        }
    }
}
