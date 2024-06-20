using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public class ListChangedEventArgs: EventArgs
    {
        public Dictionary<string, object> UpdatedList { get; }

        public ListChangedEventArgs(Dictionary<string, object> updatedList)
        {
            UpdatedList = updatedList;
        }
    }
}
