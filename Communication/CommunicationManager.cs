using DCS_Nexus.Communication;

namespace DCS_Nexus.Communication {
    public class CommunicationManager {
        public static IProtocolAdapter? DCSCommunicator;
        public static IProtocolAdapter? SlaveCommunicator;

        public static void Start(CommunicationType type) {
            switch (type) {
                case CommunicationType.TCP:
                    throw new System.NotImplementedException();
                case CommunicationType.UDP:
                    DCSCommunicator = new DCSUDPAdapter();
                    DCSCommunicator.Start();
                    throw new System.NotImplementedException();
                case CommunicationType.Multicast:
                    DCSCommunicator = new DCSUDPAdapter();
                    DCSCommunicator.Start();
                    SlaveCommunicator = new SlaveMulticastAdapter();
                    SlaveCommunicator.Start();
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        public static void Stop() {
            DCSCommunicator?.Stop();
            SlaveCommunicator?.Stop();
            DCSCommunicator = null;
            SlaveCommunicator = null;
        }

        public static bool IsRunning {
            get => DCSCommunicator != null && SlaveCommunicator != null;
        }
    }
}