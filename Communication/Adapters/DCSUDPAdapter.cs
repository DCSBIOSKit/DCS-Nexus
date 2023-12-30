using static Util.Logger;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;

using static DCS_Nexus.Communication.CommunicationManager;

namespace DCS_Nexus.Communication {
    public class DCSUDPAdapter : IProtocolAdapter
    {
        const int receiveQueueSize = 5; // Has to be kept small to avoid TCP slaves from running out of memory.
        const int sendQueueSize = 128;

        UdpClient? ReceiveClient = null;
        UdpClient? SendClient = null;

        private Thread? receiveThread;
        private bool stopReceiveThread = false;
        private MessageQueue<DCSMessage> receiveQueue = new(receiveQueueSize);

        private Thread? sendThread;
        private bool stopSendThread = false;
        private MessageQueue<DCSMessage> sendQueue = new(sendQueueSize);

        public CommunicationType Type => CommunicationType.UDP;

        public void Start()
        {
            Log($"Starting {GetType().Name}");
            
            stopReceiveThread = false;
            receiveQueue = new(receiveQueueSize);
            receiveThread = new Thread(new ThreadStart(this.Receive));
            receiveThread.Start();

            stopSendThread = false;
            sendQueue = new(sendQueueSize);
            sendThread = new Thread(new ThreadStart(this.Send));
            sendThread.Start();
        }

        public void Stop()
        {
            Log($"Stopping {GetType().Name}");
            stopReceiveThread = true;
            stopSendThread = true;
            ReceiveClient?.Close();
            ReceiveClient = null;
            SendClient?.Close();
            SendClient = null;
        }

        public void Send(byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public event System.Action<byte[]>? Received = delegate {};

        private void Receive()
        {
            Log($"Starting {GetType().Name} receive thread");

            ReceiveClient = new UdpClient(5010);
            ReceiveClient?.JoinMulticastGroup(IPAddress.Parse("239.255.50.10"));

            while (!stopReceiveThread && ReceiveClient != null)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 0);
                    byte[] receivedData = ReceiveClient.Receive(ref remoteEP);

                    DCSMessage message = new DCSMessage(receivedData);
                    receiveQueue.Enqueue(message);

                    // Log("Received: " + message.Printable);
                }
                catch (SocketException e)
                {
                    Log($"Socket exception: {e.Message}");
                }

                Thread.Sleep(2);
            }

            Log($"Stopping {GetType().Name} receive thread");
        }

        private void Send()
        {
            Log($"Starting {GetType().Name} send thread");

            SendClient = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7778);

            while (!stopSendThread)
            {
                DCSMessage? message = sendQueue.Dequeue();
                
                if (message is not null)
                {
                    try
                    {
                        // Get the data from the DCSMessage
                        byte[] dataToSend = message.Data;

                        // Send the data
                        SendClient.Send(dataToSend, dataToSend.Length, endPoint);

                        //Log($"Sent message with {dataToSend.Length} bytes.");
                    }
                    catch (SocketException e)
                    {
                        Log($"Socket exception: {e.Message}");
                    }
                }
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