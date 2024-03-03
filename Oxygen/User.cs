namespace Oxygen
{
    internal class User
    {
        public string Name { get; private set; }
        public byte[] Password { get; private set; }
        public int Id { get; private set; }

        public User(string name, byte[] password, int id)
        {
            Name = name;
            Password = password;
            Id = id;
        }

        public static bool CheckPassword(User user, byte[] password)
        {
            if (user.Password.Length != password.Length)
            {
                return false;
            }

            for (int i = 0; i < user.Password.Length; i++)
            {
                if (user.Password[i] != password[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
