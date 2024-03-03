using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oxygen
{
    internal class Users
    {
        private readonly Dictionary<string, User> users = new Dictionary<string, User>();
        private readonly Dictionary<string, User> apiUsers = new Dictionary<string, User>();
        private int userId;

        private const string userFile = @"Data\users.data";
        private const string apiKeysFile = @"Data\api_keys.data";

        private static Users? instance;

        public static Users Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Users();
                    instance.LoadUsers();
                }

                return instance;
            }
        }

        public void LoadUsers()
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }

            if (File.Exists(userFile))
            {
                using (FileStream stream = File.OpenRead(userFile))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int numUsers = reader.ReadInt32();
                        for (int i = 0; i < numUsers; i++)
                        {
                            int id = reader.ReadInt32();
                            string username = reader.ReadString();
                            int passwordLength = reader.ReadInt32();
                            byte[] password = new byte[passwordLength];
                            reader.Read(password, 0, passwordLength);

                            userId = Math.Max(userId, id + 1);

                            users.Add(username, new User(username, password, id));
                        }
                    }
                }
            }

            if (File.Exists(apiKeysFile))
            {
                using (FileStream stream = File.OpenRead(apiKeysFile))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int numUsers = reader.ReadInt32();
                        for (int i = 0; i < numUsers; i++)
                        {
                            string username = reader.ReadString();
                            string api_key = reader.ReadString();

                            User? user = GetUserByName(username);

                            if (user != null)
                            {
                                apiUsers.Add(api_key, user);
                            }
                        }
                    }
                }
            }
        }

        private void WriteUserData()
        {
            using (FileStream stream = File.OpenWrite(userFile))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(users.Count);
                    foreach (var user in users)
                    {
                        writer.Write(user.Value.Id);
                        writer.Write(user.Value.Name);
                        writer.Write(user.Value.Password.Length);
                        writer.Write(user.Value.Password);
                    }
                }
            }

            using (FileStream stream = File.OpenWrite(apiKeysFile))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(apiUsers.Count);

                    foreach (var user in apiUsers)
                    {
                        writer.Write(user.Value.Name);
                        writer.Write(user.Key);
                    }
                }
            }
        }

        public void RevokeAPIKey(string username)
        {
            List<string> keys = new List<string>();
            foreach (var api_key in this.apiUsers)
            {
                if (api_key.Value.Name == username)
                {
                    keys.Add(api_key.Key);
                }
            }

            foreach (var api_key in keys)
            {
                apiUsers.Remove(api_key);
            }

            WriteUserData();
        }

        public string CreateAPIKey(string username)
        {
            Guid guid = Guid.NewGuid();
            string apiKey = Convert.ToBase64String(guid.ToByteArray());

            User? user = GetUserByName(username);

            if (user != null)
            {
                apiUsers.Add(apiKey, user);
                Audit.Instance.Log("API key generated for user {0}.", username);
                WriteUserData();
            }

            return apiKey;
        }

        public User? CreateUser(string name, byte[] password)
        {
            User user = new User(name, password, userId++);
            users.Add(name, user);
            Audit.Instance.Log("New user created {0}.", name);
            WriteUserData();
            return user;
        }

        public User? CreateUser(string name, string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);

            byte[]? hashedBytes = null;
            using (var sha256 = SHA256.Create())
            {
                hashedBytes = sha256.ComputeHash(bytes);
            }

            return CreateUser(name, hashedBytes);
        }

        public bool DeleteUser(string username)
        {
            if (users.Remove(username))
            {
                Audit.Instance.Log("User deleted {0}.", username);
                WriteUserData();
                return true;
            }
            return false;
        }

        public User? GetUserByName(string username)
        {
            users.TryGetValue(username, out User? user);
            return user;
        }

        public User? GetUserByAPIKey(string apiKey)
        {
            apiUsers.TryGetValue(apiKey, out User? user);
            return user;
        }

        public IList<User> UserList
        {
            get
            {
                List<User> users = new List<User>();
                foreach (var user in this.users)
                {
                    users.Add(user.Value);
                }
                return users;
            }
        }
    }
}
