using DCS_Nexus.Communication;
using DCS_Nexus.Model;

namespace DCS_Nexus.Communication {
    public class CommunicationManager {
        public static IProtocolAdapter? DCSAdapter;
        public static IProtocolAdapter? SlaveAdapter;

        public static void Start(CommunicationType DCSType, CommunicationType SlaveType) {
            StartDCS(DCSType);
            StartSlaves(SlaveType);
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

        public static void StartSlaves(CommunicationType type)
        {
            switch (type) {
                case CommunicationType.TCP:
                    throw new System.NotImplementedException();
                case CommunicationType.UDP:
                    throw new System.NotImplementedException();
                case CommunicationType.Multicast:
                    SlaveAdapter = new SlaveMulticastAdapter();
                    SlaveAdapter.Start();
                    break;
                default:
                    throw new System.NotImplementedException();
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

        public static void StopSlaves() {
            SlaveAdapter?.Stop();
            SlaveAdapter = null;
        }

        public static bool IsRunning {
            get => DCSAdapter != null && SlaveAdapter != null;
        }

        public static void SendSlaveMessage(SlaveMessage message)
        {
            SlaveAdapter?.EnqueueMessage(message);
        }   
    }
}