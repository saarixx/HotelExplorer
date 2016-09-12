using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HotelDataCrawler.Model;
using Newtonsoft.Json;
using System.Threading;
using HotelDatabase.Entities;

namespace HotelExplorerGUI
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                SearchButton.IsEnabled = false;
                cancellationTokenSource.Cancel();
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            cancellationTokenSource = new CancellationTokenSource();
            SearchButton.Content = "Terminate";
            //
            string hotelName = HotelNameTextBox.Text;
            Hotel[] hotelEntities;
            HotelSearchResult hotelSearchResult = null;
            using (HotelDBEntitiesConn context = new HotelDBEntitiesConn())
            {
                hotelEntities = await context.Hotels.Where(x => x.Name == hotelName).ToArrayAsync(cancellationTokenSource.Token);
            }
            if (hotelEntities.Length > 0)
            {
                hotelSearchResult = new HotelSearchResult
                {
                    SearchName = hotelName
                };
                foreach (Hotel hotelEntity in hotelEntities)
                {
                    HotelData hotelData = JsonConvert.DeserializeObject<HotelData>(hotelEntity.Data);
                    hotelSearchResult.Hotels.Add(hotelData);
                }
            }
            else
            {
                HotelDataCrawler.HotelDataCrawler crawler = new HotelDataCrawler.HotelDataCrawler();
                try
                {
                    hotelSearchResult = await crawler.SearchHotels(hotelName, cancellationTokenSource.Token);
                    using (HotelDBEntitiesConn context = new HotelDBEntitiesConn())
                    {
                        foreach (HotelData hotelData in hotelSearchResult.Hotels)
                        {
                            Hotel hotelEntity = new Hotel
                            {
                                Name = (string)hotelData.GetParameterValue(HotelParameterType.Name),
                                WebSite = (int)hotelData.WebSite,
                                Data = JsonConvert.SerializeObject(hotelData, new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                })
                            };
                            context.Hotels.Add(hotelEntity);
                        }
                        await context.SaveChangesAsync(cancellationTokenSource.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(cancellationTokenSource.IsCancellationRequested ? "Terminated" : "Finished")
                .Append(" in ").Append(sw.Elapsed).AppendLine();
            if (hotelSearchResult != null)
            {
                sb.Append(JsonConvert.SerializeObject(hotelSearchResult, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
            }

            LogTextBox.Text = sb.ToString();
            SearchButton.Content = "Search";
            SearchButton.IsEnabled = true;
            cancellationTokenSource = null;
        }
    }
}
