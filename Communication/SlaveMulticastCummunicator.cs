using static Util.Logger;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Google.Protobuf;

namespace DCS_Nexus.Communication {
    public class SlaveMulticastCommunicator : ICommunicator
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

            UdpClient udpClient = new UdpClient(7779);
            udpClient.JoinMulticastGroup(IPAddress.Parse("239.255.50.10"));

            while (!stopReceiveThread)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpClient.Receive(ref remoteEP);

                DCSMessage message = new DCSMessage(receivedData);
                receiveQueue.Enqueue(message);

                // Log total messages
                Log($"Received {receiveQueue.Count} messages");
                //Log("Received: " + message.Printable);
            }

            Log($"Stopping {GetType().Name} receive thread");
        }

        private void Send()
        {
            Log($"Starting {GetType().Name} send thread");

            IPAddress group = IPAddress.Parse("239.255.50.10");
            int port = 7779;
            IPEndPoint endPoint = new IPEndPoint(group, port);

            // Create socket
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                DontFragment = true,
                MulticastLoopback = false,
                Ttl = 3,
                ExclusiveAddressUse = false,
            };
            udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 0x10);
            udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(group));
            //udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0)); // Not required
            
            while (!stopSendThread)
            {
                DCSMessage? message = DCSCommunicator.shared.DequeueMessage();
                if (message is not null)
                {
                    Log($"Sending message: {message.Printable}");

                    SlaveMessage slaveMessage = new SlaveMessage {
                        Data = message.ByteString
                    };
                    
                    udpSocket.SendTo(slaveMessage.ToByteArray(), endPoint);
                }
            }

            Log($"Stopping {GetType().Name} send thread");

            udpSocket.Close();
        }

        public bool HasMessages => throw new System.NotImplementedException();

        public DCSMessage? DequeueMessage()
        {
            return receiveQueue.Dequeue();
        }

        public void EnqueueMessage(DCSMessage message)
        {
            Log($"Enqueueing message: {message.Printable}");
            sendQueue.Enqueue(message);
        }
    }
}