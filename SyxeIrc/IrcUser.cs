
namespace SyxeIrc
{
    public class IrcUser
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public string Mode { get; internal set; }

        internal IrcUser(string host)
        {
            if (!host.Contains("@") && !host.Contains("!"))
                name = host;
            else
            {
                string[] mask = host.Split('@', '!');
                name = mask[0];
            }
        }
        public IrcUser(string name, string password)
        {
            this.name = name;
            this.password = password;
        }

        public bool isModerator()
        {
            if (Mode.Contains("o"))
                return true;
            else
                return false;
        }

        public bool Match(string name)
        {
            return this.name.ToLower() == name.ToLower();
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
