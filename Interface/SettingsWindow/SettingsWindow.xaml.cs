using System;
using System.Windows;
using System.Windows.Controls;
using DCS_Nexus.Communication;
using static Util.Logger;

namespace DCS_Nexus
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // Unsubscribe from the events
            TCPCheckBox.Checked -= SlaveMode_Checked;
            TCPCheckBox.Unchecked -= SlaveMode_Unchecked;
            UDPCheckBox.Checked -= SlaveMode_Checked;
            UDPCheckBox.Unchecked -= SlaveMode_Unchecked;
            MulticastCheckBox.Checked -= SlaveMode_Checked;
            MulticastCheckBox.Unchecked -= SlaveMode_Unchecked;

            // Initialize DCS Mode ComboBox
            DCSModeComboBox.SelectedIndex = (int)Settings.Default.DCSCommunicationType;

            // Initialize Slave Communication Checkboxes
            TCPCheckBox.IsChecked = Settings.Default.SlaveCommunicationTypes.Contains(CommunicationType.TCP);
            UDPCheckBox.IsChecked = Settings.Default.SlaveCommunicationTypes.Contains(CommunicationType.UDP);
            MulticastCheckBox.IsChecked = Settings.Default.SlaveCommunicationTypes.Contains(CommunicationType.Multicast);

            // Re-subscribe to the events
            TCPCheckBox.Checked += SlaveMode_Checked;
            TCPCheckBox.Unchecked += SlaveMode_Unchecked;
            UDPCheckBox.Checked += SlaveMode_Checked;
            UDPCheckBox.Unchecked += SlaveMode_Unchecked;
            MulticastCheckBox.Checked += SlaveMode_Checked;
            MulticastCheckBox.Unchecked += SlaveMode_Unchecked;
        }

        private void DCSMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DCSModeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedText = selectedItem.Content.ToString();

                switch (selectedText)
                {
                    case "TCP":
                        MessageBox.Show("TCP mode is coming soon.");
                        DCSModeComboBox.SelectedIndex = 1; // Defaulting to UDP if TCP is not implemented
                        break;
                    case "UDP":
                        Log("Switching DCS to UDP mode");
                        CommunicationManager.StopDCS();
                        CommunicationManager.StartDCS(CommunicationType.UDP);
                        break;
                }

                Settings.Default.DCSCommunicationType = (CommunicationType)Enum.Parse(typeof(CommunicationType), selectedText);
                Settings.Default.Save();
            }
        }

        private void SlaveMode_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSlaveCommunicationTypes();
        }

        private void SlaveMode_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSlaveCommunicationTypes();
        }

        private void UpdateSlaveCommunicationTypes()
        {
            Settings.Default.SlaveCommunicationTypes.Clear();

            if (TCPCheckBox.IsChecked == true)
                Settings.Default.SlaveCommunicationTypes.Add(CommunicationType.TCP);

            if (UDPCheckBox.IsChecked == true)
                Settings.Default.SlaveCommunicationTypes.Add(CommunicationType.UDP);

            if (MulticastCheckBox.IsChecked == true)
                Settings.Default.SlaveCommunicationTypes.Add(CommunicationType.Multicast);

            Settings.Default.Save();

            // Restart slave communications with new settings
            CommunicationManager.StopSlaves();
            CommunicationManager.StartSlaves(Settings.Default.SlaveCommunicationTypes.ToArray());
        }
    }
}
