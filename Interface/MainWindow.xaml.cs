using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DCS_Nexus.Communication;
using DCS_Nexus.Model;
using static Util.Logger;

namespace DCS_Nexus
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private SlaveManager SlaveManager = SlaveManager.Shared;
        
        private string _statusText;

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (value != _statusText)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                DataContext = this;
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            SlaveManager.GenerateMockSlaves();
            SlaveManager.Slaves.CollectionChanged += Slaves_CollectionChanged;
            listView.ItemsSource = SlaveManager.Slaves;
            UpdateStatusText();
        }

        private void Slaves_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            StatusText = $"{SlaveManager.Slaves.Count} slaves";
            OnPropertyChanged(nameof(StatusText)); // Notify the UI that the property has changed
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (listView.SelectedItem is Slave selectedSlave)
            {
                statusBarText.Text = $"Selected: {selectedSlave.ID}";
            }
            */
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
                selectedSlave.OpenDetailWindow();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();

            //SlaveManager.GenerateMockSlaves();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (CommunicationManager.IsRunning)
            {
                CommunicationManager.Stop();
                startStopButton.Content = "Start";
            }
            else
            {
                // TODO: Read from Settings
                CommunicationManager.Start(CommunicationType.UDP, CommunicationType.Multicast);
                startStopButton.Content = "Stop";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Stop all communication threads
            CommunicationManager.Stop();

            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
