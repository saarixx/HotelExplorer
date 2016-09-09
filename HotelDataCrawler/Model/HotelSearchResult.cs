using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelDataCrawler.Model
{
    public class HotelSearchResult
    {
        public string SearchName { get; set; }
        public List<HotelData> Hotels = new List<HotelData>();
    }
}
