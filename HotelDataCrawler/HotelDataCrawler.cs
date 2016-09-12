using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotelDataCrawler.Model;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace HotelDataCrawler
{
    public class HotelDataCrawler
    {
        public async Task<HotelSearchResult> SearchHotels(string hotelName, CancellationToken cancellationToken)
        {
            HotelSearchResult result = new HotelSearchResult
            {
                SearchName = hotelName
            };
            List<Task<List<HotelData>>> tasks = new List<Task<List<HotelData>>>();
            tasks.Add(SearchTopHotels(hotelName, cancellationToken));
            tasks.Add(SearchTripAdvisor(hotelName, cancellationToken));
            tasks.Add(SearchBooking(hotelName, cancellationToken));
            tasks.Add(SearchTezTour(hotelName, cancellationToken));
            await Task.WhenAll(tasks);
            foreach (Task<List<HotelData>> task in tasks)
            {
                List<HotelData> hotels = await task;
                result.Hotels.AddRange(hotels);
            }
            return result;
        }

        public async Task<List<HotelData>> SearchTopHotels(string hotelName, CancellationToken cancellationToken)
        {
            List<HotelData> result = new List<HotelData>();
            HttpClient http = new HttpClient();
            string url = "http://www.tophotels.ru/main/hotels//?srch=1&CO=&name=" + Uri.EscapeDataString(hotelName);
            string source;
            using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
            {
                source = await response.Content.ReadAsStringAsync();
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(source);
            IList<HtmlNode> hotelNodes = document.DocumentNode.QuerySelectorAll(".catalogs-item");
            for (int i = 0; i < hotelNodes.Count; i++)
            {
                HotelData hotelData = new HotelData
                {
                    WebSite = WebSite.TopHotels
                };
                result.Add(hotelData);
                //
                HtmlNode node = hotelNodes[i];
                HtmlNode titleNode = node.QuerySelector(".catalogs-ttl a");
                if (titleNode != null)
                {
                    string curHotelName = GetDecodedInnerText(titleNode);
                    hotelData.AddParameter(HotelParameterType.Name, curHotelName);
                    //
                    HtmlAttribute attribute = titleNode.Attributes["href"];
                    if (attribute != null)
                    {
                        string hrefStr = attribute.Value;
                        const string prefix = "/main/hotel/";
                        if (hrefStr.StartsWith(prefix) && hrefStr.EndsWith("/"))
                        {
                            string hotelId = hrefStr.Substring(prefix.Length, hrefStr.Length - prefix.Length - 1);
                            hotelData.AddParameter(HotelParameterType.Id, hotelId);
                        }
                    }
                }
                //
                HtmlNode ratingNode = node.QuerySelector(".page-ttls-hotel-rating");
                if (ratingNode != null)
                {
                    string ratingStr = GetDecodedInnerText(ratingNode);
                    ratingStr = ratingStr.Replace(',', '.');
                    float rating;
                    float.TryParse(ratingStr, out rating);
                    hotelData.AddParameter(HotelParameterType.Rating, rating);
                }
                //
                HtmlNode reviewNode = node.QuerySelector(".catalogs-per-a[href$=reviews/], .catalogs-per-a[href$=reviews]");
                if (reviewNode != null)
                {
                    string reviewStr = GetDecodedInnerText(reviewNode);
                    int pos = reviewStr.LastIndexOf(' ');
                    if (pos > 0)
                    {
                        reviewStr = reviewStr.Substring(pos + 1);
                    }
                    int reviewsNum;
                    if (int.TryParse(reviewStr, out reviewsNum))
                    {
                        hotelData.AddParameter(HotelParameterType.ReviewsNum, reviewsNum);
                    }
                }
                //
                HtmlNode photoNode = node.QuerySelector(".catalogs-per a[href$='/gallery']");
                if (photoNode != null)
                {
                    string photoStr = GetDecodedInnerText(photoNode);
                    int pos = photoStr.LastIndexOf(' ');
                    if (pos > 0)
                    {
                        photoStr = photoStr.Substring(pos + 1);
                    }
                    int photoNum;
                    if (int.TryParse(photoStr, out photoNum))
                    {
                        hotelData.AddParameter(HotelParameterType.PhotoNum, photoNum);
                    }
                }
            }
            return result;
        }

        public async Task<List<HotelData>> SearchTripAdvisor(string hotelName, CancellationToken cancellationToken)
        {
            List<HotelData> result = new List<HotelData>();
            HttpClient http = new HttpClient();
            string url = "https://www.tripadvisor.com/Search?geo&redirect&q=" + Uri.EscapeDataString(hotelName);
            string source;
            using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
            {
                source = await response.Content.ReadAsStringAsync();
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(source);
            IList<HtmlNode> hotelNodes = document.DocumentNode.QuerySelectorAll(".result");
            for (int i = 0; i < hotelNodes.Count; i++)
            {
                HotelData hotelData = new HotelData
                {
                    WebSite = WebSite.TripAdvisor
                };
                result.Add(hotelData);
                //
                HtmlNode node = hotelNodes[i];
                HtmlNode titleNode = node.QuerySelector(".title");
                if (titleNode != null)
                {
                    string curHotelName = GetDecodedInnerText(titleNode);
                    hotelData.AddParameter(HotelParameterType.Name, curHotelName);
                    //
                    HtmlAttribute attribute = titleNode.Attributes["onclick"];
                    if (attribute != null)
                    {
                        string linkStr = attribute.Value;
                        const string prefix = "/Hotel_Review-";
                        int pos = linkStr.IndexOf(prefix, StringComparison.Ordinal);
                        if (pos > 0)
                        {
                            linkStr = linkStr.Substring(pos + prefix.Length);
                        }
                        pos = linkStr.IndexOf(".html", StringComparison.Ordinal);
                        if (pos > 0)
                        {
                            string hotelId = linkStr.Substring(0, pos);
                            hotelData.AddParameter(HotelParameterType.Id, hotelId);
                        }
                        // https://www.tripadvisor.com/Hotel_Review-g297969-d508059-Reviews-Rixos_Premium_Tekirova-Tekirova_Kemer_Turkish_Mediterranean_Coast.html
                    }
                }
                //
                HtmlNode ratingNode = node.QuerySelector("img.sprite-ratings");
                if (ratingNode != null)
                {
                    HtmlAttribute attribute = ratingNode.Attributes["alt"];
                    if (attribute != null)
                    {
                        string ratingStr = attribute.Value;
                        int pos = ratingStr.IndexOf(' ');
                        if (pos > 0)
                        {
                            ratingStr = ratingStr.Substring(0, pos);
                        }
                        float rating;
                        float.TryParse(ratingStr, out rating);
                        hotelData.AddParameter(HotelParameterType.Rating, rating);
                    }
                }
                //
                HtmlNode reviewNode = node.QuerySelector("a.review-count");
                if (reviewNode != null)
                {
                    string reviewStr = GetDecodedInnerText(reviewNode);
                    int pos = reviewStr.IndexOf(' ');
                    if (pos > 0)
                    {
                        reviewStr = reviewStr.Substring(0, pos);
                    }
                    int reviewsNum;
                    if (int.TryParse(reviewStr, NumberStyles.Any, CultureInfo.InvariantCulture, out reviewsNum))
                    {
                        hotelData.AddParameter(HotelParameterType.ReviewsNum, reviewsNum);
                    }
                }
            }
            if (result.Count > 1 && string.Equals((string)result[0].GetParameterValue(HotelParameterType.Name),
                hotelName, StringComparison.InvariantCultureIgnoreCase))
            {
                HotelData hotelData = result[0];
                result.Clear();
                result.Add(hotelData);
            }
            return result;
        }

        public async Task<List<HotelData>> SearchBooking(string hotelName, CancellationToken cancellationToken)
        {
            List<HotelData> result = new List<HotelData>();
            HttpClient http = new HttpClient();
            string url = "http://www.booking.com/search.en-gb.html?ss=" + Uri.EscapeDataString(hotelName)
                + ";ss_all=0;ss_raw=" + Uri.EscapeDataString(hotelName);
            string source;
            using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
            {
                source = await response.Content.ReadAsStringAsync();
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(source);
            HtmlNode node = document.DocumentNode.QuerySelector(".disam-single-result");
            if (node == null)
            {
                return result;
            }
            url = "http://www.booking.com" + node.Attributes["data-url"].Value;
            using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
            {
                source = await response.Content.ReadAsStringAsync();
            }
            document = new HtmlDocument();
            document.LoadHtml(source);

            IList<HtmlNode> hotelNodes = document.DocumentNode.QuerySelectorAll(".sr_item");
            for (int i = 0; i < hotelNodes.Count; i++)
            {
                HotelData hotelData = new HotelData
                {
                    WebSite = WebSite.Booking
                };
                result.Add(hotelData);
                //
                node = hotelNodes[i];
                HtmlNode titleNode = node.QuerySelector(".sr-hotel__name");
                if (titleNode != null)
                {
                    string curHotelName = GetDecodedInnerText(titleNode);
                    hotelData.AddParameter(HotelParameterType.Name, curHotelName);
                }
                //
                HtmlNode linkNode = node.QuerySelector("a.hotel_name_link");
                if (linkNode != null)
                {
                    HtmlAttribute attribute = linkNode.Attributes["href"];
                    // http://www.booking.com/hotel/tr/rixos-tekirova-antalya.en-gb.html?label=gen173nr-1DCAso5AFCFnJpeG9zLXRla2lyb3ZhLWFudGFseWFILmIFbm9yZWZo6QGIAQGYAS64AQbIAQzYAQPoAQGoAgM;sid=9cbded17af2be68ff41049be24480dd0;checkin=2016-09-14;checkout=2016-09-24;ucfs=1;group_adults=2;group_children=2;age=7;age=2;req_adults=2;req_children=2;req_age=7;req_age=2;room1=A,A,2,7;highlighted_blocks=18665302_91466309_0_85_0;all_sr_blocks=18665302_91466309_0_85_0,18665302_91466309_0_85_0;dest_type=city;dest_id=-772862;srfid=6bab96d610e859f21895ef1cd1fccfeac4ff7fb8X1;highlight_room=
                    if (attribute != null)
                    {
                        string linkStr = attribute.Value;
                        if (linkStr.StartsWith("/hotel/"))
                        {
                            linkStr = linkStr.Substring(7);
                        }
                        int pos = linkStr.IndexOf(".en-gb", StringComparison.Ordinal);
                        if (pos > 0)
                        {
                            linkStr = linkStr.Substring(0, pos);
                        }
                        hotelData.AddParameter(HotelParameterType.Id, linkStr);
                    }
                }
                //
                HtmlNode ratingNode = node.QuerySelector(".rating .average");
                if (ratingNode != null)
                {
                    string ratingStr = GetDecodedInnerText(ratingNode);
                    float rating;
                    float.TryParse(ratingStr, out rating);
                    hotelData.AddParameter(HotelParameterType.Rating, rating);
                }
                //
                HtmlNode reviewNode = node.QuerySelector(".score_from_number_of_reviews");
                if (reviewNode != null)
                {
                    string reviewStr = GetDecodedInnerText(reviewNode);
                    int pos = reviewStr.IndexOf(' ');
                    if (pos > 0)
                    {
                        reviewStr = reviewStr.Substring(0, pos);
                    }
                    int reviewsNum;
                    if (int.TryParse(reviewStr, NumberStyles.Any, CultureInfo.InvariantCulture, out reviewsNum))
                    {
                        hotelData.AddParameter(HotelParameterType.ReviewsNum, reviewsNum);
                    }
                }
            }
            if (result.Count > 1)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    if (string.Equals((string)result[i].GetParameterValue(HotelParameterType.Name),
                        hotelName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        HotelData hotelData = result[i];
                        result.Clear();
                        result.Add(hotelData);
                    }
                }
            }
            return result;
        }

        public async Task<List<HotelData>> SearchTezTour(string hotelName, CancellationToken cancellationToken)
        {
            List<HotelData> result = new List<HotelData>();
            HttpClient http = new HttpClient();
            string url = "http://www.tez-tour.com/data/searchHotels.html?sortMode=1&partName=%25" + Uri.EscapeDataString(hotelName).Replace("%20", "%25")  + "&hotelTypeId=2568&hotelTypeBetter=1&pansionId=2424&pansionBetter=1";
            string source;
            using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
            {
                source = await response.Content.ReadAsStringAsync();
            }
            JObject rootObject = JObject.Parse(source);
            JArray hotelsArray = (JArray)rootObject["hotels"];
            foreach (JObject hotelObject in hotelsArray)
            {
                HotelData hotelData = new HotelData
                {
                    WebSite = WebSite.TezTour
                };
                hotelData.AddParameter(HotelParameterType.Name, hotelObject["name"].Value<string>());
                hotelData.AddParameter(HotelParameterType.Id, hotelObject["id"].Value<string>());
                result.Add(hotelData);
            }
            foreach (HotelData hotelData in result)
            {
                string hotelId = (string)hotelData.GetParameterValue(HotelParameterType.Id);
                url = "http://www.tez-tour.com/data/hotelReviews.html?id=" + hotelId + "&city=msk";
                using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
                {
                    source = await response.Content.ReadAsStringAsync();
                }
                rootObject = JObject.Parse(source);
                int reviewsNum = ((JArray)rootObject["hotelReviews"]).Count;
                hotelData.AddParameter(HotelParameterType.ReviewsNum, reviewsNum);
                //
                url = "http://www.tez-tour.com/data/hotelRating.html?id=" + hotelId + "&city=msk";
                using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
                {
                    source = await response.Content.ReadAsStringAsync();
                }
                rootObject = JObject.Parse(source);
                float rating = rootObject["average"].Value<float>();
                hotelData.AddParameter(HotelParameterType.Rating, rating);
                //
                /*url = "http://www.tez-tour.com/hotel.html?id=" + hotelId;
                using (HttpResponseMessage response = await http.GetAsync(url, cancellationToken))
                {
                    source = await response.Content.ReadAsStringAsync();
                }
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(source);*/
            }
            return result;
        }

        private static string GetDecodedInnerText(HtmlNode node)
        {
            return WebUtility.HtmlDecode(node.InnerText.Trim());
        }
    }
}
