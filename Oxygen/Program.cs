using System.Text;

namespace Oxygen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Starting server!");

                Server server = new Server(9888);
                server.AddNode(new AssetServer());
                server.AddNode(new LoginServer());
                server.AddNode(new LevelServer());

                try
                {
                    server.Start();
                }
                catch (ServerException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (args.Length == 1)
            {
                if (args[0] == "setup")
                {
                    Console.WriteLine("Performing initial setup");
                    Console.WriteLine("Creating root user");
                    Console.Write("Password: ");

                    string password = string.Empty;
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

                    Users users = new Users();
                    users.CreateUser("root", password);
                }
            }
        }
    }
}
