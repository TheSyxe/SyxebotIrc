using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyxeIrc
{
    public class IrcChannel
    {
        private IrcClient Client { get; set; }

        internal string _Topic;
        public string Topic
        {
            get
            {
                return _Topic;
            }
            set
            {
                _Topic = value;
            }
        }

        public string Name { get; internal set; }
        public string Mode { get; internal set; }
        public UserCollection Users { get; set; }
        public Dictionary<char, UserCollection> UsersByMode { get; set; }

        internal IrcChannel(IrcClient client, string name)
        {
            Client = client;
            Users = new UserCollection();
            UsersByMode = new Dictionary<char, UserCollection>();
            Name = name;
        }


        public void Part()
        {
            Client.PartChannel(Name);
        }

        public void Part(string reason)
        {
            Client.PartChannel(Name);
        }

        public void SendMessage(string message)
        {
            Client.SendMessage(message, Name);
        }
    }
}
