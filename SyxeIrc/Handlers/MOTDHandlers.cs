using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyxeIrc.Events;

namespace SyxeIrc.Handlers
{
    internal static class MOTDHandlers
    {
        public static string MOTD { get; set; }

        public static void HandleMOTDStart(IrcClient client, IrcMessage message)
        {
            
            MOTD = string.Empty;
        }

        public static void HandleMOTD(IrcClient client, IrcMessage message)
        {
            if (message.Parameters.Length != 2)
                throw new IrcProtocolException("372 MOTD message is incorrectly formatted.");
            var part = message.Parameters[1].Substring(2);
            MOTD += part + Environment.NewLine;
            client.OnMOTDPartRecieved(new ServerMOTDEventArgs(part));
        }

        public static void HandleEndOfMOTD(IrcClient client, IrcMessage message)
        {
            client.OnMOTDRecieved(new ServerMOTDEventArgs(MOTD));
            client.OnConnectionComplete(new EventArgs());
        }

        

        
    }
}
