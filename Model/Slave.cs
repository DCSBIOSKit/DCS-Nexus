using System;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DCS_Nexus.Communication;
using static Util.Logger;

namespace DCS_Nexus.Model
{
    public class Slave : INotifyPropertyChanged
    {
        public ICommand RestartCommand { get; private set; }
        public ICommand DetailsCommand { get; private set; }
        public DateTime LastSeen { get; private set; } = DateTime.UtcNow;
        public DateTime LastSent { get; private set; } = DateTime.UtcNow;

        private string? _id;
        private string? _mac;
        private IPAddress? _ip;
        private int _rssi;
        private uint _freeHeap;
        private uint _loopDuration;
        private uint _cpuFreq;
        private uint _flashSize;
        
        public Slave(SlaveMessage? message = null, IPAddress? ipAddress = null) {
            RestartCommand = new RelayCommand(Restart);
            DetailsCommand = new RelayCommand(OpenDetailWindow);

            Update(message, ipAddress);
        }

        public void Update(SlaveMessage? message, IPAddress? ipAddress) {
            if (message != null)
            {
                ID = !string.IsNullOrEmpty(message.Id) ? message.Id : ID;
                Mac = !string.IsNullOrEmpty(message.Mac) ? message.Mac : Mac;

                RSSI = message.Rssi;
                FreeHeap = message.FreeHeap > 0 ? message.FreeHeap : FreeHeap;
                LoopDuration = message.LoopDuration > 0 ? message.LoopDuration : LoopDuration;
                CPUFrequency = message.CpuFreq > 0 ? message.CpuFreq : CPUFrequency;
                FlashSize = message.FlashSize > 0 ? message.FlashSize : FlashSize;
            }

            if (ipAddress != null && !ipAddress.Equals(IPAddress.Any))
            {
                IP = ipAddress;
            }

            UpdateLastSeen();
        }

        public void UpdateLastSeen()
        {
            LastSeen = DateTime.UtcNow;
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

        public uint FreeHeap
        {
            get => _freeHeap;
            set { _freeHeap = value; OnPropertyChanged(); }
        }

        public string FreeHeapKB => $"{_freeHeap / 1024:F1} KB";

        public uint LoopDuration
        {
            get => _loopDuration;
            set { _loopDuration = value; OnPropertyChanged(); }
        }

        public string LoopDurationMS => $"{_loopDuration / (double)1024:F2} ms";

        public uint CPUFrequency
        {
            get => _cpuFreq;
            set { _cpuFreq = value; OnPropertyChanged(); }
        }

        public string CPUFrequencyMhz => $"{_cpuFreq} Mhz";

        public uint FlashSize
        {
            get => _flashSize;
            set { _flashSize = value; OnPropertyChanged(); }
        }

        public string FlashSizeMB => $"{_flashSize/1024/1024} MB";

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

            CommunicationManager.SendSlaveMessage(new SlaveMessage
            {
                Id = ID,
                Type = "restart"
            });
        }
    }
}
