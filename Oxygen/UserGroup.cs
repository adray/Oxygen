using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class UserGroup
    {
        private readonly string name;
        private readonly List<User> users = new List<User>();

        public UserGroup(string name)
        {
            this.name = name;
        }

        public string Name => name;

        public IList<User> Users => users;

        public void AddUser(User user)
        {
            users.Add(user);
        }

        public void RemoveUser(User user)
        {
            users.Remove(user);
        }
    }
}
