using DCS_Nexus.Communication;
using DCS_Nexus.Model;
using System.Collections.Generic;

namespace DCS_Nexus.Communication {
    public class CommunicationManager {
        public static IProtocolAdapter? DCSAdapter;
        public static List<IProtocolAdapter> SlaveAdapters = new List<IProtocolAdapter>();

        public static void Start(CommunicationType DCSType, params CommunicationType[] SlaveTypes)
        {
            StartDCS(DCSType);
            StartSlaves(SlaveTypes);
        }

        public static void StartDCS(CommunicationType type)
        {
            switch (type)
            {
                case CommunicationType.TCP:
                    throw new System.NotImplementedException();
                case CommunicationType.UDP:
                    DCSAdapter = new DCSUDPAdapter();
                    DCSAdapter.Start();
                    break;
                case CommunicationType.Multicast:
                    throw new System.NotSupportedException();
                default:
                    throw new System.NotImplementedException();

            }
        }

        public static void StartSlaves(params CommunicationType[] types)
        {
            foreach (var type in types)
            {
                switch (type)
                {
                    case CommunicationType.TCP:
                        SlaveAdapters.Add(new SlaveTCPAdapter());
                        break;
                    case CommunicationType.UDP:
                        // Implement UDP adapter initialization if needed
                        break;
                    case CommunicationType.Multicast:
                        SlaveAdapters.Add(new SlaveMulticastAdapter());
                        break;
                    default:
                        throw new System.NotImplementedException();
                }
            }

            foreach (var adapter in SlaveAdapters)
            {
                adapter.Start();
            }
        }

        public static void Stop()
        {
            StopDCS();
            StopSlaves();
        }

        public static void StopDCS()
        {
            DCSAdapter?.Stop();
            DCSAdapter = null;
        }

        public static void StopSlaves()
        {
            foreach (var adapter in SlaveAdapters)
            {
                adapter.Stop();
            }
            SlaveAdapters.Clear();
        }

        public static bool IsRunning
        {
            get => DCSAdapter != null && SlaveAdapters.Count > 0;
        }

        public static void SendSlaveMessage(SlaveMessage message)
        {
            foreach (var adapter in SlaveAdapters)
            {
                adapter.EnqueueMessage(message);
            }
        }
    }
}