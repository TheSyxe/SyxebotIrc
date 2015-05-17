using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SyxeIrc
{
    public class UserCollection : IEnumerable<IrcUser>
    {
        private List<IrcUser> users;
        public List<IrcUser> Users
        {
            get { return users; }
            set { users = value; }
        }

        internal UserCollection()
        {
            users = new List<IrcUser>();
        }

        internal void Add(IrcUser user)
        {
            if (users.Any(u => u.Name == user.Name))
                return;
            else
                users.Add(user);
        }

        internal void Remove(IrcUser user)
        {
            if (users.Contains(user))
                users.Remove(user);
        }
        internal void Remove(string userName)
        {
            var user = users.Find(u => u.Name.ToLower() == userName.ToLower());
            if (user != null)
            {
                users.Remove(user);
            }
        }

        public bool Contains(string name)
        {
            return users.Any(u => u.Name == name);
        }

        public bool Contains(IrcUser user)
        {
            return Users.Any(u => u.Name == user.Name);
        }

        public IrcUser this[int index]
        {
            get
            {
                return users[index];
            }
        }

        public IrcUser this[string name]
        {
            get
            {
                var user = users.FirstOrDefault(u => u.Name == name);
                if (user != null)
                    return user;
                else
                    throw new KeyNotFoundException("Cannot find '" + name + "' in UserCollection. ");
            }
        }

        public IEnumerator<IrcUser> GetEnumerator()
        {
            return users.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        
    }
}
