using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static Util.Logger;

namespace DCS_Nexus.Model
{
    public class Slave : INotifyPropertyChanged
    {
        public ICommand RestartCommand { get; set; }
        public ICommand DetailsCommand { get; set; }

        private string? _id;
        private string? _mac;
        private IPAddress? _ip;
        private int _rssi;
        private int _free_heap;
        private int _loop_duration;
        private int _cpu_freq;
        private int _flash_size;

        public Slave()
        {
            RestartCommand = new RelayCommand(Restart);
            DetailsCommand = new RelayCommand(OpenDetailWindow);
        }

        public string ID
        {
            get => _id ?? "Unknown";
            set { _id = value; OnPropertyChanged(); }
        }

        public string Mac
        {
            get => _mac ?? "Unknown";
            set { _mac = value; OnPropertyChanged(); }
        }

        public string MacUpper => $"{Mac.ToUpper()}";

        public IPAddress IP
        {
            get => _ip ?? IPAddress.None;
            set { _ip = value; OnPropertyChanged(); }
        }

        public string IPString
        {
            get => _ip?.ToString() ?? "Unknown";
        }

        public int RSSI
        {
            get => _rssi;
            set { _rssi = value; OnPropertyChanged(); }
        }

        public int RSSIPercent => _rssi + 100;

        public string RSSIText => $"{RSSIPercent}%";

        public int FreeHeap
        {
            get => _free_heap;
            set { _free_heap = value; OnPropertyChanged(); }
        }

        public string FreeHeapKB => $"{_free_heap / 1024:F1} KB";

        public int LoopDuration
        {
            get => _loop_duration;
            set { _loop_duration = value; OnPropertyChanged(); }
        }

        public string LoopDurationMS => $"{_loop_duration / (double)1024:F1} ms";

        public int CPUFrequency
        {
            get => _cpu_freq;
            set { _cpu_freq = value; OnPropertyChanged(); }
        }

        public string CPUFrequencyMhz => $"{_cpu_freq} Mhz";

        public int FlashSize
        {
            get => _flash_size;
            set { _flash_size = value; OnPropertyChanged(); }
        }

        public string FlashSizeMB => $"{_flash_size/1024/1024} MB";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Management Functions

        public string DetailWindowTitle => $"{ID} Details";

        public void OpenDetailWindow(object? parameter = null)
        {
            SlaveDetailWindow detailWindow = new SlaveDetailWindow(this);
            detailWindow.Show();
        }

        public void Restart(object? parameter = null)
        {
            Log($"Restarting {ID}");
        }
    }
}
