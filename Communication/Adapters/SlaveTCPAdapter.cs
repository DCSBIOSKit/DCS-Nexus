using static Util.Logger;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Google.Protobuf;

using static DCS_Nexus.Communication.CommunicationManager;
using System.Windows;
using DCS_Nexus.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using Makaretu.Dns;

namespace DCS_Nexus.Communication {
    public class SlaveTCPAdapter : IProtocolAdapter
    {
        private class ClientState
        {
            public Guid GUID { get; set; } = Guid.NewGuid();
            public TcpClient Client { get; set; }
            public byte[] Buffer { get; set; }
            public MemoryStream DataStream { get; set; } = new MemoryStream();
            public int ExpectedLength { get; set; } = -1;
            public NetworkStream? Stream => Client.Connected ? Client.GetStream() : null;
            public IPAddress IPAddress { get; set; }
            public int Port { get; set; }

            public ClientState(TcpClient client, byte[] buffer, IPAddress ipAddress, int port)
            {
                Client = client;
                Buffer = buffer;
                IPAddress = ipAddress;
                Port = port;
            }
        }

        const int receiveQueueSize = 256;
        const int dcsMessageQueueSize = 10;
        const int slaveMessageQueueSize = 10;

        private Thread? receiveThread;
        private ManualResetEvent stopReceiveThread = new ManualResetEvent(false);
        private MessageQueue<DCSMessage> receiveQueue = new(receiveQueueSize);

        private Thread? sendThread;
        private bool stopSendThread = false;
        private MessageQueue<DCSMessage> dcsMessageQueue = new(dcsMessageQueueSize);
        private MessageQueue<SlaveMessage> slaveMessageQueue = new(slaveMessageQueueSize);
        
        public CommunicationType Type => CommunicationType.TCP;

        private TcpListener? receiveClient;
        private ConcurrentDictionary<Guid, TcpClient> connectedClients = new ConcurrentDictionary<Guid, TcpClient>();

        private const string serviceName = "_dcs-bios._tcp";
        private const int servicePort = 7779;

        private MulticastService multicast = new MulticastService();
        private ServiceProfile profile = new ServiceProfile("DCS-Nexus", serviceName, servicePort);
        private ServiceDiscovery service = new ServiceDiscovery();

        public void Start()
        {
            Log($"Starting {GetType().Name}");

            service.Advertise(profile);
            multicast.Start();

            stopReceiveThread.Reset();
            receiveQueue = new(receiveQueueSize);
            receiveThread = new Thread(new ThreadStart(this.Receive));
            receiveThread.Start();

            stopSendThread = false;
            dcsMessageQueue = new(dcsMessageQueueSize);
            slaveMessageQueue = new(slaveMessageQueueSize);
            sendThread = new Thread(new ThreadStart(this.Send));
            sendThread.Start();
        }

        public void Stop()
        {
            Log($"Stopping {GetType().Name}");

            service.Unadvertise();
            multicast.Stop();

            stopReceiveThread.Set();
            stopSendThread = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                SlaveManager.Shared.Slaves.Clear();
            });
        }

        public event System.Action<byte[]>? Received = delegate {};

        private void Receive()
        {
            Log($"Starting {GetType().Name} receive thread");

            receiveClient = new TcpListener(IPAddress.Any, 7779);
            receiveClient.Start();
            receiveClient.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);

            stopReceiveThread.WaitOne();

            foreach (var (GUID, client) in connectedClients)
            {
                client.Close();
            }
            connectedClients.Clear();

            receiveClient.Stop();
            receiveClient = null;

            Log($"Stopping {GetType().Name} receive thread");
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            TcpClient? client = receiveClient?.EndAcceptTcpClient(ar);

            if (client is not null)
            {
                Console.WriteLine("Client connected.");

                client.Client.NoDelay = true;
                client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 0x10);

                IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                ClientState clientState = new ClientState(client, buffer, remoteEndPoint.Address, remoteEndPoint.Port);
                connectedClients.TryAdd(clientState.GUID, client);
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), clientState);
            }

            receiveClient?.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);
        }

        private void OnDataReceived(IAsyncResult ar)
        {
            ClientState state = (ClientState)ar.AsyncState;
            NetworkStream stream = state.Stream;
            int bytesRead = 0;

            try
            {
                bytesRead = stream.EndRead(ar);
            }
            catch (Exception e)
            {
                Log($"OnDataReceived Exception: {e.Message}");
                state.Client.Close();
                connectedClients.TryRemove(state.GUID, out _);

                return;
            }

            if (bytesRead > 0)
            {
                // Write the received data to the DataStream
                state.DataStream.Write(state.Buffer, 0, bytesRead);

                // Reset the position to the beginning for reading
                state.DataStream.Position = 0;

                while (state.DataStream.Length - state.DataStream.Position >= 2) // Ensure sufficient data for length
                {
                    if (state.ExpectedLength == -1) // Haven't read the length yet
                    {
                        byte[] lengthBuffer = new byte[2];
                        state.DataStream.Read(lengthBuffer, 0, 2);
                        state.ExpectedLength = BitConverter.ToInt16(lengthBuffer, 0); // Adjust for endianness if necessary

                        //Log($"Expected length: {state.ExpectedLength}");
                    }

                    if (state.ExpectedLength != -1 && state.DataStream.Length - state.DataStream.Position >= state.ExpectedLength)
                    {
                        byte[] messageBuffer = new byte[state.ExpectedLength];
                        state.DataStream.Read(messageBuffer, 0, state.ExpectedLength);

                        // Log($"Received message: {BitConverter.ToString(messageBuffer)}, length: {messageBuffer.Length}");
                        
                        ReceivedMessage(messageBuffer, state); // Process the message

                        state.ExpectedLength = -1; // Reset for the next message
                    }
                    else
                    {
                        break; // Wait for more data
                    }
                }

                // Clear the DataStream if it's fully read, or prepare it for more reading
                if (state.DataStream.Position == state.DataStream.Length)
                {
                    state.DataStream.SetLength(0); // Reset if all data has been read
                }
                else if (state.DataStream.Position > 0)
                {
                    byte[] remaining = state.DataStream.ToArray().Skip((int)state.DataStream.Position).ToArray();
                    state.DataStream.SetLength(0);
                    state.DataStream.Write(remaining, 0, remaining.Length);
                }

                // Begin another asynchronous read operation
                stream.BeginRead(state.Buffer, 0, state.Buffer.Length, new AsyncCallback(OnDataReceived), state);
            }
        }

        private void ReceivedMessage(byte[] receivedData, ClientState state)
        {
            // Decode the received byte array into a SlaveMessage object
            SlaveMessage decodedMessage = SlaveMessage.Parser.ParseFrom(receivedData);

            // Create or update the slave object
            Application.Current.Dispatcher.Invoke(() =>
            {
                Slave? slave = SlaveManager.FindSlaveByMacAddress(decodedMessage.Mac);
                
                if (slave is null)
                {
                    slave = new Slave(decodedMessage, state.IPAddress);
                    SlaveManager.Shared.Slaves.Add(slave);
                }
                else
                {
                    slave.Update(decodedMessage, state.IPAddress);
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

            Log($"Received message: {decodedMessage.Type} from {decodedMessage.Id}");
        }

        private void Send()
        {
            Log($"Starting {GetType().Name} send thread");

            DateTime lastCheckInTime = DateTime.MinValue;
            
            while (!stopSendThread)
            {
                // For each connected client:
                foreach (var (GUID, slave) in connectedClients)
                {
                    if (!slave.Connected)
                    {
                        continue;
                    }

                    // Send the next ACK/slave message in the queue
                    SlaveMessage? ack = slaveMessageQueue?.Dequeue();
                    if (ack is not null)
                    {
                        try
                        {
                            SendWithLengthPrefix(slave, ack.ToByteArray());
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
                            
                            SendWithLengthPrefix(slave, slaveMessage.ToByteArray());
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
                            SendWithLengthPrefix(slave, checkInMessage.ToByteArray());
                            lastCheckInTime = currentTime;
                            Log("Sent check-in message to " + slave.Client.RemoteEndPoint.ToString());
                        }
                        catch (SocketException e)
                        {
                            Log($"Socket exception: {e.Message}");
                        }
                    }
                }
            }

            Log($"Stopping {GetType().Name} send thread");
        }

        private void SendWithLengthPrefix(TcpClient slave, byte[] messageData)
        {
            if (slave == null || !slave.Connected)
                return;

            NetworkStream stream = slave.GetStream();
            if (!stream.CanWrite)
                return;

            // Convert message length to byte array.
            ushort messageLength = (ushort)messageData.Length;
            byte[] lengthPrefix = BitConverter.GetBytes(messageLength);

            // Create a new byte array that includes the length prefix and the message.
            byte[] dataToSend = new byte[lengthPrefix.Length + messageData.Length];
            lengthPrefix.CopyTo(dataToSend, 0);
            messageData.CopyTo(dataToSend, lengthPrefix.Length);

            slave.Client.SendAsync(dataToSend/*, SocketFlags.OutOfBand*/);
            //stream.WriteAsync(dataToSend, 0, dataToSend.Length).ConfigureAwait(false);
            //stream.FlushAsync().ConfigureAwait(false);
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