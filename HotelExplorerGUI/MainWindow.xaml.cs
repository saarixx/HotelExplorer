using System;
using System.Collections.Generic;
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
            HotelDataCrawler.HotelDataCrawler crawler = new HotelDataCrawler.HotelDataCrawler();
            HotelSearchResult hotelSearchResult = null;
            try
            {
                hotelSearchResult = await crawler.SearchHotels(HotelNameTextBox.Text, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // ignore
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
