using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using SyxeIrc.Events;

namespace SyxeIrc
{
    public class IrcClient : IDisposable
    {

        public delegate void MessageHandler(IrcClient client, IrcMessage message);
        private Dictionary<string, MessageHandler> Handlers { get; set; }
        public void SetHandler(string message, MessageHandler handler)
        {
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

        public IrcClient(string serverAddress, IrcUser user)
        {
            this.serverAddress = serverAddress;
            this.serverPort = 6667;
            this.user = user;
            encoding = Encoding.UTF8;
            channels = new ChannelCollection(this);
            Handlers = new Dictionary<string, MessageHandler>();
        }
        public void ConnectAsync()
        {
            tcpClient = new TcpClient();
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
            OnConnectionComplete(new EventArgs());
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
            if (message.Command == "PRIVMSG")
            {
                var privMessage = new PrivateMessage(message);
                OnPrivateMessageRecieved(new PrivateMessageEventArgs(message));
                if (message.Parameters[0].StartsWith("#"))
                    OnChannelMessageRecieved(new PrivateMessageEventArgs(message));
            }
            if (Handlers.ContainsKey(message.Command.ToUpper()))
                Handlers[message.Command.ToUpper()](this, message);
        }

        public void SendRawMessage(string message, params object[] format)
        {
            if (networkStream != null)
            {
                if (format != null)
                    message = string.Format(message, format);
                var data = encoding.GetBytes(message + "\r\n");
                networkStream.BeginWrite(data, 0, data.Length, MessageSent, message);
            }
        }
        private void MessageSent(IAsyncResult result)
        {
            if (networkStream != null)
            {
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

                }

                OnRawMessageSent(new RawMessageEventArgs((string)result.AsyncState, true));
            }
        }

        public void SendMessage(string message, params string[] destinations)
        {
            const string illegalCharacters = "\r\n\0";
            if (!destinations.Any()) throw new InvalidOperationException("Message must have at least one target.");
            if (illegalCharacters.Any(message.Contains)) throw new ArgumentException("Illegal characters are present in message.", "message");
            string to = string.Join(",", destinations);
            SendRawMessage("PRIVMSG {0} :{1}", to, message);
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
            if (Channels.Contains(channel))
            {
                throw new InvalidOperationException("Client is not already present in channel.");
            }
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

        public void Dispose()
        {
            TcpClient.Close();
            NetworkStream.Close();
        }
    }
}
