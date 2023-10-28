using System;

namespace DCS_Nexus.Communication {
    public enum CommunicationType
    {
        TCP,
        UDP,
        Multicast
    }

    public interface ICommunicator
    {
        void Start();
        void Stop();
        void Send(byte[] data);
        event Action<byte[]> Received;

        bool HasMessages { get; }
        DCSMessage? DequeueMessage();
        
        void EnqueueMessage(DCSMessage message);
    }
}