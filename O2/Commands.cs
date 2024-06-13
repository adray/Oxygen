using Oxygen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace O2
{
    internal class Command
    {
        protected static ClientConnection StartClient()
        {
            string hostname = Environment.GetEnvironmentVariable("OXYGEN_HOST") ?? "localhost";
            if (!int.TryParse(Environment.GetEnvironmentVariable("OXYGEN_PORT"), out int port))
            {
                port = 9888;
            }

            return new ClientConnection(hostname, port);
        }

        static string? LoadAPIKey()
        {
            if (File.Exists(@".o2\api-key"))
            {
                byte[] key = File.ReadAllBytes(@".o2\api-key");
                return Encoding.UTF8.GetString(key);
            }

            return null;
        }

        private static void LoginWithAPIKey(ClientConnection cli)
        {
            string? apikey = LoadAPIKey();
            if (apikey != null)
            {
                cli.Login(apikey);
            }
            else
            {
                Console.WriteLine("Login");
                string? username = UserNamePrompt();
                if (username != null)
                {
                    byte[] password = PasswordPrompt();
                    if (password != null)
                    {
                        cli.Login(username, password);
                    }
                }
            }
        }

        protected static string? UserNamePrompt()
        {
            Console.Write("Username: ");
            return Console.ReadLine();
        }

        protected static byte[] PasswordPrompt()
        {
            string password = string.Empty;

            Console.Write("Password: ");
            for (; ; )
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    password = password.Remove(password.Length - 1);
                }
                else
                {
                    password += keyInfo.KeyChar;
                }
            }
            Console.WriteLine();

            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[]? hashedBytes = null;

            using (var sha256 = SHA256.Create())
            {
                hashedBytes = sha256.ComputeHash(bytes);
            }

            // Clear password bytes.
            Array.Fill<byte>(bytes, 0);

            return hashedBytes;
        }

        public virtual void Invoke(string[] args)
        {
            var client = StartClient();

            try
            {
                LoginWithAPIKey(client);
                Invoke(client, args.ToArray());
            }
            catch (ClientException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public virtual void Invoke(ClientConnection client, string[] args)
        {
            // Do nothing
        }
    }

    internal class Commands
    {
        private class Node
        {
            private readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>();
            private Command? command;

            public void RegisterCommand(Command command)
            {
                this.command = command;
            }

            public void AddNode(string name, Node node)
            {
                this.nodes.Add(name, node);
            }

            public Node? GetNode(string name)
            {
                nodes.TryGetValue(name, out Node? child);
                return child;
            }

            public bool Invoke(string[] args, int idx)
            {
                if (args.Length == idx)
                {
                    if (this.command != null)
                    {
                        this.command.Invoke(args);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                string name = args[idx];
                if (this.nodes.TryGetValue(name, out Node? node) && node != null)
                {
                    return node.Invoke(args, idx + 1);
                }
                else if (this.command != null)
                {
                    this.command.Invoke(args);
                    return true;
                }

                return false;
            }
        }

        private readonly Node rootNode = new Node();

        public void AddCommand(Command command, params string[] path)
        {
            Node node = rootNode;
            foreach (string name in path)
            {
                Node? childNode = node.GetNode(name);
                if (childNode == null)
                {
                    childNode = new Node();
                    node.AddNode(name, childNode);
                }

                node = childNode;
            }

            node.RegisterCommand(command);
        }

        public bool Invoke(string[] path)
        {
            return rootNode.Invoke(path, 0);
        }
    }
}
