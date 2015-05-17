using System;

namespace SyxeIrc.Events
{
    public class ModeChangeEventArgs : EventArgs
    {
        public string Target { get; set; }
        public IrcUser User { get; set; }
        public string Change { get; set; }

        public ModeChangeEventArgs(string target, IrcUser user, string change)
        {
            Target = target;
            User = user;
            Change = change;
        }
    }
}
