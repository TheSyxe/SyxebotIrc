using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyxeIrc.Events;

namespace SyxeIrc.Handlers
{
    internal static class IrcMessageHandlers
    {
        public static void RegisterDefaultHandlers(IrcClient client)
        {
            client.SetHandler("PING", HandlePing);
            client.SetHandler("PRIVMSG", HandlePrivmsg);
            client.SetHandler("MODE", HandleMode);
            client.SetHandler("JOIN", ChannelHandlers.HandleJoin);
            client.SetHandler("PART", ChannelHandlers.HandlePart);
        }
        public static void HandlePing(IrcClient client, IrcMessage message)
        {
            client.SendRawMessage("PONG :{0}", message.Parameters[0]);
        }


        public static void HandlePrivmsg(IrcClient client, IrcMessage message)
        {
            var eventArgs = new PrivateMessageEventArgs(message);
            client.OnPrivateMessageRecieved(eventArgs);
            if (eventArgs.PrivateMessage.IsChannelMessage)
            {
                try
                {
                    // Populate this user's hostname and user from the message
                    // TODO: Merge all users from all channels into one list and keep references to which channels they're in
                    var channel = client.Channels[eventArgs.PrivateMessage.Source];
                    var u = channel.Users[eventArgs.PrivateMessage.User.Name];

                }
                catch { /* silently ignored */ }
                client.OnChannelMessageRecieved(eventArgs);
            }
            else
                client.OnUserMessageRecieved(eventArgs);
        }
        public static void HandleMode(IrcClient client, IrcMessage message)
        {
            string target, mode = null;
            int i = 2;
            if (message.Command == "MODE")
            {
                target = message.Parameters[0];
                mode = message.Parameters[1];
            }
            else
            {
                target = message.Parameters[1];
                mode = message.Parameters[2];
                i++;
            }
            // Handle change
            bool add = true;
            if (target.StartsWith("#"))
            {
                var channel = client.Channels[target];
                try
                {
                    foreach (char c in mode)
                    {
                        if (c == '+')
                        {
                            add = true;
                            continue;
                        }
                        if (c == '-')
                        {
                            add = false;
                            continue;
                        }
                        if (channel.Mode == null)
                            channel.Mode = string.Empty;
                        if (!channel.UsersByMode.ContainsKey(c)) channel.UsersByMode.Add(c, new UserCollection());
                        var user = new IrcUser(message.Parameters[i]);
                        if (add)
                        {
                            if (!channel.UsersByMode[c].Contains(user.Name))
                                channel.UsersByMode[c].Add(user);
                        }
                        else
                        {
                            if (channel.UsersByMode[c].Contains(user.Name))
                                channel.UsersByMode[c].Remove(user);
                        }
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, new IrcUser(message.Prefix),
                            (add ? "+" : "-") + c.ToString() + " " + message.Parameters[i++]));


                        if (add)
                        {
                            if (!channel.Mode.Contains(c))
                                channel.Mode += c.ToString();
                        }
                        else
                            channel.Mode = channel.Mode.Replace(c.ToString(), string.Empty);
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, new IrcUser(message.Prefix),
                            (add ? "+" : "-") + c.ToString()));

                    }
                }
                catch { }
                
            }
            else
            {
                // TODO: Handle user modes other than ourselves?
                foreach (char c in mode)
                {
                    if (add)
                    {
                        if (!client.User.Mode.Contains(c))
                            client.User.Mode += c;
                    }
                    else
                        client.User.Mode = client.User.Mode.Replace(c.ToString(), string.Empty);
                }
            }
        }



    }
}
