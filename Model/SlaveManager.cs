using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Timers;
using System.Windows;

namespace DCS_Nexus.Model
{
    class SlaveManager
    {
        public static SlaveManager Shared = new SlaveManager();

        public ObservableCollection<Slave> Slaves { get; set; } = new ObservableCollection<Slave>();

        private Timer _checkStaleSlavesTimer;

        public SlaveManager()
        {
            _checkStaleSlavesTimer = new Timer(1000);
            _checkStaleSlavesTimer.Elapsed += CheckStaleSlaves;
            _checkStaleSlavesTimer.AutoReset = true;
            _checkStaleSlavesTimer.Start();
        }

        private void CheckStaleSlaves(object? sender, ElapsedEventArgs e)
        {
            TimeSpan maxAge = TimeSpan.FromSeconds(3);
            RemoveStaleSlaves(maxAge);
        }

        public void RemoveStaleSlaves(TimeSpan maxAge)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var staleSlaves = Slaves.Where(slave => DateTime.UtcNow - slave.LastSeen > maxAge).ToList();

                foreach (var staleSlave in staleSlaves)
                {
                    Slaves.Remove(staleSlave);
                }
            });
        }

        public static Slave? FindSlaveByMacAddress(string macAddress)
        {
            return Shared.Slaves.FirstOrDefault(slave => slave.Mac == macAddress);
        }

        public void GenerateMockSlaves()
        {
            for (int i = 0; i < 32; i++)
            {
                Shared.Slaves.Add(new Slave { ID = $"mock-{i}", IP = IPAddress.Any, Mac = "00:00:00:00:00:00", RSSI = Random.Shared.Next(10, 90) * -1, FreeHeap = (uint)Random.Shared.Next(100000, 280000), LoopDuration = (uint)Random.Shared.Next(50, 100), CPUFrequency = 240, FlashSize = 8388608 });
            }
        }
    }
}