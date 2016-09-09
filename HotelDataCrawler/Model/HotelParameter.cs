using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HotelDataCrawler.Model
{
    public class HotelParameter
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public HotelParameterType Type { get; set; }
        
        public string Name { get; set; }

        public object Value { get; set; }
    }
}
