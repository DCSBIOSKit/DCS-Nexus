using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DCS_Nexus.Model
{
    class SlaveManager
    {
        public static SlaveManager Shared = new SlaveManager();

        public ObservableCollection<Slave> Slaves { get; set; } = new ObservableCollection<Slave>();

        public void GenerateMockSlaves()
        {
            for (int i = 0; i < 32; i++)
            {
                Shared.Slaves.Add(new Slave { ID = $"mock-{i}", Mac = "00:00:00:00:00:00", RSSI = Random.Shared.Next(10, 90) * -1, FreeHeap = Random.Shared.Next(100000, 280000), LoopDuration = Random.Shared.Next(50, 100), CPUFrequency = 240, FlashSize = 8388608 });
            }
        }
    }
}