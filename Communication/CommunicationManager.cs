using DCS_Nexus.Communication;

namespace DCS_Nexus.Communication {
    public class CommunicationManager {
        public static ICommunicator? DCSCommunicator;
        public static ICommunicator? SlaveCommunicator;

        public static void Start(CommunicationType type) {
            switch (type) {
                case CommunicationType.TCP:
                    throw new System.NotImplementedException();
                case CommunicationType.UDP:
                    DCSCommunicator = new DCSUDPCommunicator();
                    DCSCommunicator.Start();
                    throw new System.NotImplementedException();
                case CommunicationType.Multicast:
                    DCSCommunicator = new DCSUDPCommunicator();
                    DCSCommunicator.Start();
                    SlaveCommunicator = new SlaveMulticastCommunicator();
                    SlaveCommunicator.Start();
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        public static void Stop() {
            DCSCommunicator?.Stop();
            SlaveCommunicator?.Stop();
        }
    }
}