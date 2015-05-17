
namespace SyxeIrc
{
    public class PrivateMessage
    {
        private IrcUser user;
        public IrcUser User
        {
            get { return user; }
            set { user = value; }
        }

        private string message;
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        private string source;
        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        private bool isChannelMessage;
        public bool IsChannelMessage
        {
            get { return isChannelMessage; }
            set { isChannelMessage = value; }
        }

        public PrivateMessage(IrcMessage message)
        {
            this.source = message.Parameters[0];
            this.message = message.Parameters[1];
            user = new IrcUser(message.Prefix);

            if (source.StartsWith("#"))
                isChannelMessage = true;
            else
                source = user.Name;
        }
    }
}
