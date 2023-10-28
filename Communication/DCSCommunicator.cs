using System.Windows;

namespace DCS_Nexus.Communication {
    public class DCSCommunicator {
        public static readonly DCSCommunicator shared = new DCSCommunicator();

        private ICommunicator? _communicator;

        public void Start(CommunicationType type) {
            switch (type) {
                case CommunicationType.TCP:
                    throw new System.NotImplementedException();
                case CommunicationType.UDP:
                    _communicator = new DCSUDPCommunicator();
                    _communicator.Start();
                    break;
                case CommunicationType.Multicast:
                    throw new System.NotSupportedException();
                default:
                    throw new System.NotImplementedException();
            }
        }

        public void Stop() {
            _communicator?.Stop();
        }

        public bool HasMessages => !_communicator?.HasMessages ?? false;
        
        public DCSMessage? DequeueMessage() {
            return _communicator?.DequeueMessage();
        }
        
        public void EnqueueMessage(DCSMessage message) {
            _communicator?.EnqueueMessage(message);
        }
    }
}
