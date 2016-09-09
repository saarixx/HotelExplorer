using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HotelDataCrawler.Model
{
    public class HotelData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public WebSite WebSite { get; set; }

        public readonly List<HotelParameter> Parameters = new List<HotelParameter>();

        public void AddParameter(HotelParameterType type, object value)
        {
            Parameters.Add(new HotelParameter
            {
                Type = type,
                Value = value
            });
        }

        public void AddParameter(string name, object value)
        {
            Parameters.Add(new HotelParameter
            {
                Type = HotelParameterType.Additional,
                Name = name,
                Value = value
            });
        }

        public object GetParameterValue(HotelParameterType hotelParameterType)
        {
            foreach (HotelParameter parameter in Parameters)
            {
                if (parameter.Type == hotelParameterType)
                {
                    return parameter.Value;
                }
            }
            return null;
        }
    }
}
