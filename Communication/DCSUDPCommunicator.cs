using static Util.Logger;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace DCS_Nexus.Communication {
    public class DCSUDPCommunicator : ICommunicator
    {
        private Thread? receiveThread;
        private bool stopReceiveThread = false;
        private MessageQueue<DCSMessage> receiveQueue = new(1000);

        private Thread? sendThread;
        private bool stopSendThread = false;
        private MessageQueue<DCSMessage> sendQueue = new(1000);

        public void Start()
        {
            Log($"Starting {GetType().Name}");
            
            stopReceiveThread = false;
            receiveQueue = new(1000);
            receiveThread = new Thread(new ThreadStart(this.Receive));
            receiveThread.Start();

            stopSendThread = false;
            sendQueue = new(1000);
            sendThread = new Thread(new ThreadStart(this.Send));
            sendThread.Start();
        }

        public void Stop()
        {
            Log($"Stopping {GetType().Name}");
            stopReceiveThread = true;
            stopSendThread = true;
        }

        public void Send(byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public event System.Action<byte[]>? Received = delegate {};

        private void Receive()
        {
            Log($"Starting {GetType().Name} receive thread");

            UdpClient udpClient = new UdpClient(5010);
            udpClient.JoinMulticastGroup(IPAddress.Parse("239.255.50.10"));

            while (!stopReceiveThread)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 0);
                byte[] receivedData = udpClient.Receive(ref remoteEP);

                DCSMessage message = new DCSMessage(receivedData);
                receiveQueue.Enqueue(message);

                Log("Received: " + message.Printable);
            }

            Log($"Stopping {GetType().Name} receive thread");
        }

        private void Send()
        {
            Log($"Starting {GetType().Name} send thread");
            while (!stopSendThread)
            {
                Thread.Sleep(1000);
                Log("Send thread is still alive");
            }

            Log($"Stopping {GetType().Name} send thread");
        }

        public bool HasMessages => !receiveQueue.IsEmpty;

        public DCSMessage? DequeueMessage()
        {
            return receiveQueue.Dequeue();
        }

        public void EnqueueMessage(DCSMessage message)
        {
            sendQueue.Enqueue(message);
        }
    }
}