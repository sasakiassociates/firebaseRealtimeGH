using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public class DictChangedEventArgs: EventArgs
    {
        public Dictionary<string, object> UpdatedDict { get; }
        public Dictionary<string, object> ChangedItems { get; }

        public DictChangedEventArgs(Dictionary<string, object> updatedDict)
        {
            UpdatedDict = updatedDict;
            ChangedItems = new Dictionary<string, object>();
        }
    }
}
