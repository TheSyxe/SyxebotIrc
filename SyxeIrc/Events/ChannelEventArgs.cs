using System;

namespace SyxeIrc.Events
{
    public class ChannelEventArgs : EventArgs
    {
        public IrcChannel Channel { get; internal set; }

        public ChannelEventArgs(IrcChannel channel)
        {
            Channel = channel;
        }
    }
}
