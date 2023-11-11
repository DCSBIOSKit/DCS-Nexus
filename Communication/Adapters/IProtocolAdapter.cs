using System;
using DCS_Nexus.Model;

namespace DCS_Nexus.Communication {
    public enum CommunicationType
    {
        TCP,
        UDP,
        Multicast
    }

    public interface IProtocolAdapter
    {
        CommunicationType Type { get; }

        void Start();
        void Stop();
        void Send(byte[] data);
        event Action<byte[]> Received;

        bool HasMessages { get; }
        DCSMessage? DequeueMessage();
        
        void EnqueueMessage(DCSMessage message);
        void EnqueueMessage(SlaveMessage message) {}
    }
}
