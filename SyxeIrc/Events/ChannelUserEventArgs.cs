﻿using System;

namespace SyxeIrc.Events
{
    public class ChannelUserEventArgs : EventArgs
    {
        public IrcChannel Channel { get; internal set; }
        public IrcUser User { get; set; }

        public ChannelUserEventArgs(IrcChannel channel, IrcUser user)
        {
            Channel = channel;
            User = user;
        }
    }
}
