using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using SyxeIrc.Events;
using SyxeIrc.Handlers;

namespace SyxeIrc
{
    public class IrcClient : IDisposable
    {

        public delegate void MessageHandler(IrcClient client, IrcMessage message);
        private Dictionary<string, MessageHandler> Handlers { get; set; }
        public void SetHandler(string message, MessageHandler handler)
        {
#if DEBUG
            // This is the default behavior if 3rd parties want to handle certain messages themselves
            // However, if it happens from our own code, we probably did something wrong
            if (Handlers.ContainsKey(message.ToUpper()))
                Console.WriteLine("Warning: {0} handler has been overwritten", message);
#endif
            message = message.ToUpper();
            Handlers[message] = handler;
        }
        private IrcUser user;
        public IrcUser User
        {
            get { return user; }
            set { user = value; }
        }

        private ChannelCollection channels;
        public ChannelCollection Channels
        {
            get { return channels; }
            set { channels = value; }
        }

        private string serverAddress;
        public string ServerAddress
        {
            get { return serverAddress; }
            set { serverAddress = value; }
        }

        private int serverPort;
        public int ServerPort
        {
            get { return serverPort; }
            set { serverPort = value; }
        }

        private TcpClient tcpClient;
        public TcpClient TcpClient
        {
            get { return tcpClient; }
            set { tcpClient = value; }
        }

        private Stream networkStream;
        public Stream NetworkStream
        {
            get { return networkStream; }
            set { networkStream = value; }
        }

        private Encoding encoding;
        public Encoding Encoding
        {
            get { return encoding; }
            set { encoding = value; }
        }
        private byte[] readBuffer { get; set; }
        private int readBufferIndex { get; set; }

        public RequestManager RequestManager { get; set; }

        private ConcurrentQueue<string> WriteQueue { get; set; }

        private int messagesSentInLast30Seconds;
        private bool IsWriting { get; set; }

        public IrcClient(string serverAddress, IrcUser user)
        {
            this.serverAddress = serverAddress;
            this.serverPort = 6667;
            this.user = user;
            encoding = Encoding.UTF8;
            channels = new ChannelCollection(this);
            Handlers = new Dictionary<string, MessageHandler>();
            WriteQueue = new ConcurrentQueue<string>();
            messagesSentInLast30Seconds = 0;
            IrcMessageHandlers.RegisterDefaultHandlers(this);
        }
        public void ConnectAsync()
        {
            tcpClient = new TcpClient();
            var checkQueue = new Timer(1000);
            var floodQueue = new Timer(30000);
            floodQueue.Elapsed += (sender, e) =>
                {
                    messagesSentInLast30Seconds = 0;
                };
            checkQueue.Elapsed += (sender, e) =>
            {
                string nextMessage;
                if (WriteQueue.Count > 0)
                {
                    while (!WriteQueue.TryDequeue(out nextMessage) && messagesSentInLast30Seconds < 19) 
                    SendRawMessage(nextMessage);
                }
            };
            checkQueue.Start();
            floodQueue.Start();
            tcpClient.BeginConnect(serverAddress, serverPort, ConnectComplete, null);

        }

        private void ConnectComplete(IAsyncResult result)
        {
            tcpClient.EndConnect(result);
            networkStream = tcpClient.GetStream();
            if (readBuffer == null)
            {
                readBuffer = new byte[1024];
            }

            networkStream.BeginRead(readBuffer, readBufferIndex, readBuffer.Length, DataRecieved, null);

            if (!string.IsNullOrEmpty(user.Password))
                SendRawMessage("PASS {0}", user.Password);
            SendRawMessage("NICK {0}", user.Name);
            SendRawMessage("USER {0} hostname servername :{1}", user.Name, user.Name);
        }

        private void DataRecieved(IAsyncResult result)
        {
            int length = 0;
            try
            {
                length = networkStream.EndRead(result) + readBufferIndex;
            }
            catch (IOException e)
            {
                var socketException = e.InnerException as SocketException;
                if (socketException != null)
                    OnNetworkError(new NetworkErrorEventArgs(socketException.SocketErrorCode));
                else
                    throw;
                return;
            }
            readBufferIndex = 0;
            while (length > 0)
            {
                int messageLength = Array.IndexOf(readBuffer, (byte)'\n', 0, length);
                if (messageLength == -1)
                {
                    readBufferIndex = length;
                    break;
                }

                messageLength++;
                var message = encoding.GetString(readBuffer, 0, messageLength - 2);
                HandleMessage(message);
                Array.Copy(readBuffer, messageLength, readBuffer, 0, length - messageLength);
                length -= messageLength;
            }
            networkStream.BeginRead(readBuffer, readBufferIndex, readBuffer.Length - readBufferIndex, DataRecieved, null);
        }

        private void HandleMessage(string rawMessage)
        {

            OnRawMessageRecieved(new RawMessageEventArgs(rawMessage, false));
            var message = new IrcMessage(rawMessage);
            if (Handlers.ContainsKey(message.Command.ToUpper()))
                Handlers[message.Command.ToUpper()](this, message);
        }

        public void SendRawMessage(string message, params object[] format)
        {
            if (networkStream == null)
            {
                OnNetworkError(new NetworkErrorEventArgs(SocketError.NotConnected));
                return;
            }
            
                message = string.Format(message, format);
                var data = encoding.GetBytes(message + "\r\n");
                if (!IsWriting || messagesSentInLast30Seconds > 19)
                {
                    IsWriting = true;
                    NetworkStream.BeginWrite(data, 0, data.Length, MessageSent, message);
                }
                else
                    WriteQueue.Enqueue(message);
                
            
        }
        private void MessageSent(IAsyncResult result)
        {
            if (networkStream == null)
            {
                OnNetworkError(new NetworkErrorEventArgs(SocketError.NotConnected));
                IsWriting = false;
                return;
            }

            try
            {
                networkStream.EndWrite(result);
            }
            catch (IOException e)
            {
                var socketException = e.InnerException as SocketException;
                if (socketException != null)
                    OnNetworkError(new NetworkErrorEventArgs(socketException.SocketErrorCode));
                else
                    throw;
                return;
            }
            finally
            {
                IsWriting = false;
            }
            
            OnRawMessageSent(new RawMessageEventArgs((string)result.AsyncState, true));

            string nextMessage;
            if (WriteQueue.Count > 0)
            {
                while (!WriteQueue.TryDequeue(out nextMessage)) ;
                SendRawMessage(nextMessage);
            }
        }

        public void SendMessage(string message, params string[] destinations)
        {
            const string illegalCharacters = "\r\n\0";
            if (!destinations.Any()) throw new InvalidOperationException("Message must have at least one target.");
            if (illegalCharacters.Any(message.Contains)) throw new ArgumentException("Illegal characters are present in message.", "message");
            string to = string.Join(",", destinations);
            string s = "PRIVMSG " + to + " :" + message;
           SendRawMessage(s);
        }

        public void PartChannel(string channel)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("PART {0}", channel);
            Channels.Remove(Channels[channel]);
        }

        public void JoinChannel(string channel)
        {
            if (!channel.StartsWith("#"))
            {
                channel = "#" + channel.ToLower();
            }
            else
            {
                channel = channel.ToLower();
            }
            Channels.Add(new IrcChannel(this, channel));
            //if (Channels.Contains(channel))
            //{
            //    throw new InvalidOperationException("Client is not already present in channel.");
            //}
            SendRawMessage("JOIN {0}", channel);
        }
        public event EventHandler<NetworkErrorEventArgs> NetworkError;
        protected internal virtual void OnNetworkError(NetworkErrorEventArgs e)
        {
            if (NetworkError != null) NetworkError(this, e);
        }

        public event EventHandler<RawMessageEventArgs> RawMessageSent;
        protected internal virtual void OnRawMessageSent(RawMessageEventArgs e)
        {
            if (RawMessageSent != null) RawMessageSent(this, e);
        }

        public event EventHandler<RawMessageEventArgs> RawMessageRecieved;
        protected internal virtual void OnRawMessageRecieved(RawMessageEventArgs e)
        {
            if (RawMessageRecieved != null) RawMessageRecieved(this, e);
        }

        public event EventHandler<PrivateMessageEventArgs> PrivateMessageRecieved;
        protected internal virtual void OnPrivateMessageRecieved(PrivateMessageEventArgs e)
        {
            if (PrivateMessageRecieved != null) PrivateMessageRecieved(this, e);
        }

        public event EventHandler<PrivateMessageEventArgs> ChannelMessageRecieved;
        protected internal virtual void OnChannelMessageRecieved(PrivateMessageEventArgs e)
        {
            if (ChannelMessageRecieved != null) ChannelMessageRecieved(this, e);
        }

        public event EventHandler<PrivateMessageEventArgs> UserMessageRecieved;
        protected internal virtual void OnUserMessageRecieved(PrivateMessageEventArgs e)
        {
            if (UserMessageRecieved != null) UserMessageRecieved(this, e);
        }

        public event EventHandler<ModeChangeEventArgs> ModeChanged;
        protected internal virtual void OnModeChanged(ModeChangeEventArgs e)
        {
            if (ModeChanged != null) ModeChanged(this, e);
        }

        public event EventHandler<ChannelUserEventArgs> UserJoinedChannel;
        protected internal virtual void OnUserJoinedChannel(ChannelUserEventArgs e)
        {
            if (UserJoinedChannel != null) UserJoinedChannel(this, e);
        }

        public event EventHandler<ChannelUserEventArgs> UserPartedChannel;
        protected internal virtual void OnUserPartedChannel(ChannelUserEventArgs e)
        {
            if (UserPartedChannel != null) UserPartedChannel(this, e);
        }

        public event EventHandler<EventArgs> ConnectionComplete;
        protected internal virtual void OnConnectionComplete(EventArgs e)
        {
            if (ConnectionComplete != null) ConnectionComplete(this, e);
        }

        public event EventHandler<ServerMOTDEventArgs> MOTDPartRecieved;
        protected internal virtual void OnMOTDPartRecieved(ServerMOTDEventArgs e)
        {
            if (MOTDPartRecieved != null) MOTDPartRecieved(this, e);
        }
        public event EventHandler<ServerMOTDEventArgs> MOTDRecieved;
        protected internal virtual void OnMOTDRecieved(ServerMOTDEventArgs e)
        {
            if (MOTDRecieved != null) MOTDRecieved(this, e);
        }

        public void Dispose()
        {
            TcpClient.Close();
            NetworkStream.Close();
        }
    }
}
