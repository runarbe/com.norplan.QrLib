using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace norplan.adm.qrlib
{
    public class MyAbuDhabiNetResponse
    {
        [JsonProperty(PropertyName = "status")]
        public string status { get; set; }
        [JsonProperty(PropertyName = "x")]
        public string x { get; set; }
        [JsonProperty(PropertyName = "y")]
        public string y { get; set; }
        [JsonProperty(PropertyName = "areaAbbreviation")]
        public string areaAbbreviation { get; set; }
        [JsonProperty(PropertyName = "areaName")]
        public string areaName { get; set; }

    }
}
