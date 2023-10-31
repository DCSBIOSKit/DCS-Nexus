using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DCS_Nexus.Communication;
using DCS_Nexus.Model;
using static Util.Logger;

namespace DCS_Nexus
{
    public partial class MainWindow : Window
    {
        private SlaveManager SlaveManager = SlaveManager.Shared;
        
        public MainWindow()
        {
            InitializeComponent();

            SlaveManager.GenerateSampleSlaves();
            listView.ItemsSource = SlaveManager.Slaves;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem is Slave selectedSlave)
            {
                statusBarText.Text = $"Selected: {selectedSlave.ID}";
            }
        }

        private void ListView_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listView.SelectedItem is Slave selectedSlave)
            {
                // Perform right-click action, e.g., show context menu
                MessageBox.Show($"Right-clicked on: {selectedSlave.ID}");
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listView.SelectedItem is Slave selectedSlave)
            {
                SlaveDetailWindow detailWindow = new SlaveDetailWindow();
                detailWindow.DataContext = selectedSlave;
                detailWindow.Show();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open settings dialog or perform other action
            // MessageBox.Show("Settings clicked");

            SlaveManager.GenerateSampleSlaves();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Stop all communication threads
            CommunicationManager.Stop();

            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
    }
}
