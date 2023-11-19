using System.Windows;
using DCS_Nexus.Model;

namespace DCS_Nexus {
    public partial class SlaveDetailWindow : Window
    {
        private Slave _slave;

        public SlaveDetailWindow(Slave slave)
        {
            InitializeComponent();

            _slave = slave;
            DataContext = _slave;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            
            _slave.Restart();
        }
    }
}