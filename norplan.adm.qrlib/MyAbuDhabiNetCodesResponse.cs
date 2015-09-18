using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace norplan.adm.qrlib
{
    public class MyAbuDhabiNetCodesResponse
    {
        [JsonProperty(PropertyName = "time")]
        public double Time { get; set; }
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
        [JsonProperty(PropertyName = "codes")]
        public List<string> Codes { get; set; }
    }
}
