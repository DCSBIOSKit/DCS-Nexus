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

            switch (CommunicationManager.DCSAdapter?.Type)
            {
                case CommunicationType.TCP:
                    DCSModeComboBox.SelectedIndex = 0;
                    break;
                case CommunicationType.UDP:
                    DCSModeComboBox.SelectedIndex = 1;
                    break;
            }

            switch (CommunicationManager.SlaveAdapter?.Type)
            {
                case CommunicationType.TCP:
                    SlaveModeComboBox.SelectedIndex = 0;
                    break;
                case CommunicationType.UDP:
                    SlaveModeComboBox.SelectedIndex = 1;
                    break;
                case CommunicationType.Multicast:
                    SlaveModeComboBox.SelectedIndex = 2;
                    break;
            }
        }

        private void DCSMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string selectedText = selectedItem.Content.ToString();

                switch (selectedText)
                {
                    case "TCP":
                        MessageBox.Show("TCP mode is coming soon.");
                        comboBox.SelectedIndex = 1;
                        break;
                    case "UDP":
                        Log("Switching DCS to UDP mode");
                        CommunicationManager.StopDCS();
                        CommunicationManager.StartDCS(CommunicationType.UDP);
                        break;
                }
            }
        }

        private void SlaveMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string selectedText = selectedItem.Content.ToString();

                switch (selectedText)
                {
                    case "TCP":
                        MessageBox.Show("TCP mode is coming soon.");
                        comboBox.SelectedIndex = 2;
                        break;
                    case "UDP":
                        MessageBox.Show("UDP mode is coming soon.");
                        comboBox.SelectedIndex = 2;
                        break;
                    case "Multicast":
                        Log("Switching Slaves to Multicast mode");
                        CommunicationManager.StopSlaves();
                        CommunicationManager.StartSlaves(CommunicationType.Multicast);
                        break;
                }
            }
        }
    }
}
