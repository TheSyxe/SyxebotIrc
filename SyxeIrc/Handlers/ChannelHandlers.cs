using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyxeIrc.Events;

namespace SyxeIrc.Handlers
{
    internal static class ChannelHandlers
    {
        public static void HandleJoin(IrcClient client, IrcMessage message)
        {
            IrcChannel channel = null;
            if (client.User.Name == new IrcUser(message.Prefix).Name)
            {
                channel = new IrcChannel(client, message.Parameters[0]);
                client.Channels.Add(channel);
            }
            else
            {
                channel = client.Channels[message.Parameters[0]];
                channel.Users.Add(new IrcUser(message.Prefix));
            }
            if (channel != null)
                client.OnUserJoinedChannel(new ChannelUserEventArgs(channel, new IrcUser(message.Prefix)));
        }

        public static void HandlePart(IrcClient client, IrcMessage message)
        {
            if (!client.Channels.Contains(message.Parameters[0]))
                return; // we already parted the channel, ignore

            if (client.User.Match(message.Prefix)) // We've parted this channel
                client.Channels.Remove(client.Channels[message.Parameters[0]]);
            else // Someone has parted a channel we're already in
            {
                var user = new IrcUser(message.Prefix).Name;
                var channel = client.Channels[message.Parameters[0]];
                if (channel.Users.Contains(user))
                    channel.Users.Remove(user);
                foreach (var mode in channel.UsersByMode)
                {
                    if (mode.Value.Contains(user))
                        mode.Value.Remove(user);
                }
                client.OnUserPartedChannel(new ChannelUserEventArgs(client.Channels[message.Parameters[0]], new IrcUser(message.Prefix)));
            }
        }
    }
}
