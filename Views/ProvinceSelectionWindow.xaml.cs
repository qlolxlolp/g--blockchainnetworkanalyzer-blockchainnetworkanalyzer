using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BlockchainNetworkAnalyzer.Core.Models;
using BlockchainNetworkAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Views
{
    public partial class ProvinceSelectionWindow : Window
    {
        private readonly ILogger<ProvinceSelectionWindow> _logger;
        private readonly ISPService _ispService;
        public string SelectedProvince { get; private set; }
        public List<string> SelectedCities { get; private set; } = new List<string>();
        public List<IPRangeInfo> IPRanges { get; private set; } = new List<IPRangeInfo>();

        public ProvinceSelectionWindow()
        {
            InitializeComponent();
            _logger = App.LoggerFactory.CreateLogger<ProvinceSelectionWindow>();
            _ispService = new ISPService();
            LoadProvinces();
        }

        private void LoadProvinces()
        {
            var provinces = IranianProvinces.GetProvinceNames();
            ProvinceListBox.ItemsSource = provinces;
        }

        private void ProvinceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProvinceListBox.SelectedItem is string selectedProvince)
            {
                SelectedProvince = selectedProvince;
                var cities = IranianProvinces.GetCitiesByProvince(selectedProvince);
                CityListBox.ItemsSource = cities;
            }
        }

        private async void FetchIPRangesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedProvince))
            {
                MessageBox.Show("Please select a province first.", "Selection Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as System.Windows.Controls.Button;
            button.IsEnabled = false;
            button.Content = "در حال دریافت... / Fetching...";

            try
            {
                var selectedCities = CityListBox.SelectedItems.Cast<string>().ToList();
                if (selectedCities.Count == 0)
                {
                    selectedCities = new List<string> { null }; // All cities
                }

                IPRanges.Clear();

                foreach (var city in selectedCities)
                {
                    var ranges = await _ispService.GetIPRangesAsync(SelectedProvince, city);
                    IPRanges.AddRange(ranges);
                }

                MessageBox.Show($"Retrieved {IPRanges.Count} IP ranges for {SelectedProvince}.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching IP ranges");
                MessageBox.Show($"Error fetching IP ranges: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "دریافت IP Ranges / Fetch IP Ranges";
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectedCities = CityListBox.SelectedItems.Cast<string>().ToList();
            DialogResult = true;
            Close();
        }
    }
}

