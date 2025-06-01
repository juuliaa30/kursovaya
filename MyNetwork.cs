using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace MyNetwork
{
    public enum MessageSendMode : byte
    {
        Unreliable,
        Reliable
    }

    public class Message
    {
        public const int MaxSize = 1250;
        public MessageSendMode SendMode { get; private set; }
        public byte[] Data { get; private set; } = new byte[MaxSize];
        public int Position { get; set; }

        public Message(MessageSendMode mode)
        {
            SendMode = mode;
        }

        public void Add(byte value) => Data[Position++] = value;
        public byte GetByte() => Data[Position++];

        public void Add(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            Add((ushort)bytes.Length);
            foreach (byte b in bytes)
            {
                Add(b);
            }
        }

        public string GetString()
        {
            ushort length = GetUShort();
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = GetByte();
            }
            return Encoding.UTF8.GetString(bytes);
        }

        public void Add(ushort value)
        {
            Add((byte)(value >> 8));
            Add((byte)value);
        }

        public ushort GetUShort()
        {
            return (ushort)((GetByte() << 8) | GetByte());
        }
    }

    public class UdpConnection
    {
        private UdpClient socket;
        private IPEndPoint remoteEndPoint;

        public UdpConnection(string ip, int port)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new UdpClient();
        }

        public void Send(Message message)
        {
            socket.Send(message.Data, message.Position, remoteEndPoint);
        }

        public Message Receive()
        {
            var data = socket.Receive(ref remoteEndPoint);
            return new Message(MessageSendMode.Unreliable);
        }
    }

    public class Client
    {
        private UdpConnection connection;

        public void Connect(string ip, int port)
        {
            connection = new UdpConnection(ip, port);
        }

        public void Send(Message message)
        {
            connection.Send(message);
        }

        public Message Receive()
        {
            return connection.Receive();
        }
    }

    public class Server
    {
        private UdpClient socket;
        private Dictionary<IPEndPoint, ClientInfo> clients = new Dictionary<IPEndPoint, ClientInfo>();

        public void Start(int port)
        {
            socket = new UdpClient(port);
            new Thread(Listen).Start();
        }

        private void Listen()
        {
            while (true)
            {
                IPEndPoint clientEndPoint = null;
                var data = socket.Receive(ref clientEndPoint);
                var message = new Message(MessageSendMode.Unreliable);

                if (!clients.ContainsKey(clientEndPoint))
                    clients[clientEndPoint] = new ClientInfo(clientEndPoint);

                HandleMessage(message, clientEndPoint);
            }
        }

        private void HandleMessage(Message message, IPEndPoint client)
        {
            byte messageType = message.GetByte();

            switch (messageType)
            {
                case 0:
                    OnClientConnected(client);
                    break;

                case 1:
                    ushort messageId = message.GetUShort();
                    OnMessageReceived(client, messageId, message);
                    break;

                case 2:
                    OnClientDisconnected(client);
                    break;

                default:
                    Console.WriteLine($"Unknown message type: {messageType}");
                    break;
            }
        }

        private void OnClientConnected(IPEndPoint client)
        {
            var clientInfo = clients[client];
            Console.WriteLine($"Client connected: {client}, ID: {clientInfo.Id}");
            var response = new Message(MessageSendMode.Reliable);
            response.Add((byte)0);
            response.Add(clientInfo.Id);
            socket.Send(response.Data, response.Position, client);
        }

        private void OnMessageReceived(IPEndPoint client, ushort messageId, Message message)
        {
            if (!clients.TryGetValue(client, out var clientInfo))
                return;

            clientInfo.UpdateActivity();

            Console.WriteLine($"Received message {messageId} from client {clientInfo.Id}");
            switch (messageId)
            {
                case 1:
                    HandlePingMessage(clientInfo, message);
                    break;

                case 2:
                    HandleChatMessage(clientInfo, message);
                    break;
            }
        }

        private void HandlePingMessage(ClientInfo client, Message message)
        {
            var pong = new Message(MessageSendMode.Unreliable);
            pong.Add((byte)1);
            pong.Add((ushort)2);
            socket.Send(pong.Data, pong.Position, client.EndPoint);
        }

        private void HandleChatMessage(ClientInfo sender, Message message)
        {
            string text = message.GetString();
            Console.WriteLine($"Chat message from {sender.Id}: {text}");

            var broadcast = new Message(MessageSendMode.Reliable);
            broadcast.Add((byte)1);
            broadcast.Add((ushort)3);
            broadcast.Add(sender.Id);
            broadcast.Add(text);

            foreach (var client in clients.Values)
            {
                if (client.Id != sender.Id)
                {
                    socket.Send(broadcast.Data, broadcast.Position, client.EndPoint);
                }
            }
        }

        private void OnClientDisconnected(IPEndPoint client)
        {
            if (clients.TryGetValue(client, out var clientInfo))
            {
                Console.WriteLine($"Client disconnected: {clientInfo.Id}");
                clients.Remove(client);
            }
        }

        public void SendToAll(Message message)
        {
            foreach (var client in clients.Keys)
            {
                socket.Send(message.Data, message.Position, client);
            }
        }
    }

    public class ClientInfo
    {
        public IPEndPoint EndPoint { get; }
        public string Name { get; set; }
        public DateTime LastActivity { get; set; }
        public ushort Id { get; }
        public short Ping { get; set; }

        private static ushort lastId = 0;

        public ClientInfo(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
            LastActivity = DateTime.Now;
            Id = ++lastId;
        }

        public void UpdateActivity()
        {
            LastActivity = DateTime.Now;
        }

        public bool IsTimedOut(TimeSpan timeout)
        {
            return (DateTime.Now - LastActivity) > timeout;
        }
    }
}