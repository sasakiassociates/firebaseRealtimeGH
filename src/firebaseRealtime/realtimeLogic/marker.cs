using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace realtimeLogic
{
    public class Marker
    {
        public string uuid { get; set; }
        [JsonProperty ("id")]
        public int id { get; set; }
        [JsonProperty ("x")]
        public int x { get; set; }
        [JsonProperty ("y")]
        public int y { get; set; }
        [JsonProperty ("rotation")]
        public float rotation { get; set; }
    }
}
