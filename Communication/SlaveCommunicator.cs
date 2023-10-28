using DCS_Nexus.Communication;

namespace DCS_Nexus {
    class SlaveCommunicator {
        public static readonly SlaveCommunicator shared = new SlaveCommunicator();

        private ICommunicator? _communicator;

        public void Start(CommunicationType type) {
            switch (type) {
                case CommunicationType.TCP:
                    throw new System.NotImplementedException();
                case CommunicationType.UDP:
                    throw new System.NotSupportedException();                    
                case CommunicationType.Multicast:
                    _communicator = new SlaveMulticastCommunicator();
                    _communicator.Start();
                    break;
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