using System;
using System.Collections.Generic;
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
using MSURandomizerLibrary.Configs;
using MSURandomizerLibrary.Services;
using MSUScripter.Services;

namespace MSUScripter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IMsuTypeService _msuTypeService;
        private MsuType _msuType;
        private int _trackOffset = 1;
        
        public MainWindow(IMsuTypeService msuTypeService)
        {
            _msuTypeService = msuTypeService;
            _msuType = _msuTypeService.MsuTypes.First();
            InitializeComponent();
            PopulateMsuTypeComboBox();
        }

        private void PopulateMsuTypeComboBox()
        {
            MsuTypeComboBox.ItemsSource = _msuTypeService.MsuTypes.Select(x => x.DisplayName);
            MsuTypeComboBox.SelectedItem = _msuType.DisplayName;
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MsuTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _msuType = _msuTypeService.GetMsuType(MsuTypeComboBox.SelectedItem as string) ??
                       _msuTypeService.MsuTypes.First();
            PopulatePageComboBox();
        }

        private void PopulatePageComboBox()
        {
            int currentPage = PageComboBox.SelectedIndex;
            
            var pages = new List<string>() { "MSU Details" };
            
            foreach (var track in _msuType.Tracks.OrderBy(x => x.Number))
            {
                pages.Add($"Track #{track.Number} - {track.Name}");
            }

            _trackOffset = _msuType.Tracks.Select(x => x.Number).Min() - 1;

            PageComboBox.ItemsSource = pages;
            PageComboBox.SelectedIndex = Math.Clamp(currentPage, 0, pages.Count - 1);
        }
    }
}