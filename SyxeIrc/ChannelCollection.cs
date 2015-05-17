using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SyxeIrc
{
    public class ChannelCollection : IEnumerable<IrcChannel>
    {
        private IrcClient Client { get; set; }
        private List<IrcChannel> Channels { get; set; }

        internal ChannelCollection(IrcClient client)
        {
            this.Channels = new List<IrcChannel>();
            this.Client = client;
        }

        internal void Add(IrcChannel channel)
        {
            if (!Channels.Any(c => c.Name == channel.Name))
                Channels.Add(channel);
        }

        internal void Remove(IrcChannel channel)
        {
            if (Channels.Contains(channel))
                Channels.Remove(channel);
        }

        public void Join(string name)
        {
            Client.JoinChannel(name);
        }

        public bool Contains(string name)
        {
            return Channels.Any(c => c.Name == name);
        }

        public IrcChannel this[int index]
        {
            get
            {
                return Channels[index];
            }
        }

        public IrcChannel this[string name]
        {
            get
            {
                var channel = Channels.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                if (channel != null)
                    return channel;
                else
                    throw new KeyNotFoundException("Cannot find '" + name + "' in ChannelCollection. ");
            }
        }

        public IEnumerator<IrcChannel> GetEnumerator()
        {
            return Channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
