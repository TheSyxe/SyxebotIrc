using System.Collections.Generic;
using System.Linq;

namespace SyxeIrc
{
    public class IrcMessage
    {
        private string rawMessage;
        public string RawMessage
        {
            get { return rawMessage; }
            set { rawMessage = value; }
        }

        private string prefix;
        public string Prefix
        {
            get { return prefix; }
            set { prefix = value; }
        }

        private string command;
        public string Command
        {
            get { return command; }
            set { command = value; }
        }

        public string[] Parameters { get; set; }

        public IrcMessage(string rawMessage)
        {
            this.rawMessage = rawMessage;
            if (rawMessage.StartsWith(":"))
            {
                prefix = rawMessage.Substring(1, rawMessage.IndexOf(' ') - 1);
                rawMessage = rawMessage.Substring(rawMessage.IndexOf(' ') + 1);
            }
            if (rawMessage.Contains(' '))
            {
                command = rawMessage.Remove(rawMessage.IndexOf(' '));
                rawMessage = rawMessage.Substring(rawMessage.IndexOf(' ') + 1);

                var parameters = new List<string>();
                while (!string.IsNullOrEmpty(rawMessage))
                {
                    if (rawMessage.StartsWith(":"))
                    {
                        parameters.Add(rawMessage.Substring(1));
                        break;
                    }
                    if (!rawMessage.Contains(' '))
                    {
                        parameters.Add(rawMessage);
                        rawMessage = string.Empty;
                        break;
                    }
                    parameters.Add(rawMessage.Remove(rawMessage.IndexOf(' ')));
                    rawMessage = rawMessage.Substring(rawMessage.IndexOf(' ') + 1);
                }
                Parameters = parameters.ToArray();
            }
            else
            {
                command = rawMessage;
                Parameters = new string[0];
            }
        }
    }

}
