using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyxeIrc;

namespace TestSyxeIrc
{
    class Program
    {
        static void Main(string[] args)
        {
            IrcClient c = new IrcClient("irc.twitch.tv", new IrcUser("ThePhoenixBot", Private.oauth));
            c.ChannelMessageRecieved += ((s, e) =>
                {
                    if (e.IrcMessage.Parameters[0].ToLower() == "!time")
                    {
                        c.SendMessage(DateTime.Now.ToShortTimeString(), e.PrivateMessage.Source);
                    }
                });

            c.ConnectionComplete += ((s, e) =>
                {
                    c.JoinChannel("#nitemarephoenix");
                });

            c.ConnectAsync();
            while (true)
            {

            }
        }

    }
}
