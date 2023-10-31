using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DCS_Nexus.Model
{
    public class Slave : INotifyPropertyChanged
    {
        public static Slave shared = new Slave {};

        private string? _id;
        private string? _mac;
        private int _rssi;
        private int _free_heap;
        private int _loop_duration;
        private int _cpu_freq;
        private int _flash_size;

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
    }
}
