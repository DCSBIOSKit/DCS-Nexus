using static Util.Logger;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Google.Protobuf;

using static DCS_Nexus.Communication.CommunicationManager;
using System.Windows;
using DCS_Nexus.Model;
using System;

namespace DCS_Nexus.Communication {
    public class SlaveMulticastAdapter : IProtocolAdapter
    {
        Socket? SendSocket = null;

        private Thread? receiveThread;
        private bool stopReceiveThread = false;
        private MessageQueue<DCSMessage> receiveQueue = new(1000);

        private Thread? sendThread;
        private bool stopSendThread = false;
        private MessageQueue<DCSMessage> dcsMessageQueue = new(1000);
        private MessageQueue<SlaveMessage> slaveMessageQueue = new(1000);
        
        public CommunicationType Type => CommunicationType.Multicast;

        public void Start()
        {
            Log($"Starting {GetType().Name}");

            stopReceiveThread = false;
            receiveQueue = new(1000);
            receiveThread = new Thread(new ThreadStart(this.Receive));
            receiveThread.Start();

            stopSendThread = false;
            dcsMessageQueue = new(1000);
            slaveMessageQueue = new(1000);
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

            while (!stopReceiveThread)
            {
                if (udpClient.Available == 0)
                {
                    continue;
                }

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                byte[] receivedData = udpClient.Receive(ref remoteEP);

                // Decode the received byte array into a SlaveMessage object
                SlaveMessage decodedMessage = SlaveMessage.Parser.ParseFrom(receivedData);

                // Create or update the slave object
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Slave? slave = SlaveManager.FindSlaveByMacAddress(decodedMessage.Mac);
                    
                    if (slave is null)
                    {
                        slave = new Slave(decodedMessage, remoteEP.Address);
                        SlaveManager.Shared.Slaves.Add(slave);
                    }
                    else
                    {
                        slave.Update(decodedMessage, remoteEP.Address);
                    }
                });

                // Process DCS messages
                if (decodedMessage.Type != "check-in")
                {
                     // Create a DCSMessage object and enqueue it
                    DCSMessage message = new DCSMessage(decodedMessage.Data.ToByteArray());
                    DCSAdapter?.EnqueueMessage(message);

                    // Enqueue an ACK message
                    slaveMessageQueue.Enqueue(new SlaveMessage {
                        Type = "ack",
                        Id = decodedMessage.Id,
                        Seq = decodedMessage.Seq
                    });
                }

                // Trigger the Received event if needed
                Received?.Invoke(receivedData);
            }

            udpClient.Close();

            Log($"Stopping {GetType().Name} receive thread");
        }

        private void Send()
        {
            Log($"Starting {GetType().Name} send thread");

            IPAddress group = IPAddress.Parse("232.0.1.3");
            int port = 7779;
            IPEndPoint endPoint = new IPEndPoint(group, port);

            // Create socket
            SendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                DontFragment = true,
                MulticastLoopback = false,
                Ttl = 3,
                ExclusiveAddressUse = false,
            };
            SendSocket?.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 0x10);
            SendSocket?.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(group));
            //SendSocket?.Bind(new IPEndPoint(IPAddress.Any, 0)); // Not required
            
            DateTime lastCheckInTime = DateTime.MinValue;
            
            while (!stopSendThread && SendSocket != null)
            {
                // Send the next ACK/slave message in the queue
                SlaveMessage ack = slaveMessageQueue?.Dequeue();
                if (ack is not null)
                {
                    try
                    {
                        SendSocket?.SendTo(ack.ToByteArray(), endPoint);
                    }
                    catch (SocketException e)
                    {
                        Log($"Socket exception: {e.Message}");
                    }
                }

                // Send the next DCS message in the queue
                DCSMessage? message = DCSAdapter?.DequeueMessage();
                if (message is not null)
                {
                    try
                    {
                        SlaveMessage slaveMessage = new SlaveMessage {
                            Type = "message",
                            Data = message.ByteString
                        };
                        
                        SendSocket?.SendTo(slaveMessage.ToByteArray(), endPoint);
                    }
                    catch (SocketException e)
                    {
                        Log($"Socket exception: {e.Message}");
                    }
                }

                // Send a check-in message every second
                DateTime currentTime = DateTime.Now;

                if ((currentTime - lastCheckInTime).TotalSeconds >= 1)
                {
                    // Send check-in message
                    SlaveMessage checkInMessage = new SlaveMessage {
                        Type = "check-in",
                    };

                    try
                    {
                        SendSocket?.SendTo(checkInMessage.ToByteArray(), endPoint);
                        lastCheckInTime = currentTime; // Update the last check-in time
                    }
                    catch (SocketException e)
                    {
                        Log($"Socket exception: {e.Message}");
                    }
                }
            }

            Log($"Stopping {GetType().Name} send thread");

            SendSocket?.Close();
            SendSocket = null;
        }

        public bool HasMessages => throw new System.NotImplementedException();

        public DCSMessage? DequeueMessage()
        {
            return receiveQueue.Dequeue();
        }

        public void EnqueueMessage(DCSMessage message)
        {
            Log($"Enqueueing message: {message.Printable}");
            dcsMessageQueue.Enqueue(message);
        }
        
        public void EnqueueMessage(SlaveMessage message)
        {
            Log($"Enqueueing message: {message.Type} to {message.Id}");
            slaveMessageQueue.Enqueue(message);
        }
    }
}