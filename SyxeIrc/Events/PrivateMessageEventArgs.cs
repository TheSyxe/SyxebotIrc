﻿using System;

namespace SyxeIrc.Events
{
    public class PrivateMessageEventArgs : EventArgs
    {
        public IrcMessage IrcMessage { get; set; }
        public PrivateMessage PrivateMessage { get; set; }

        public PrivateMessageEventArgs(IrcMessage ircMessage)
        {
            IrcMessage = ircMessage;
            PrivateMessage = new PrivateMessage(IrcMessage);
        }
    }
}
