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

                Authorizer.LoadPermissions();
                Authorizer.LoadAuthorizationData();

                Server server = new Server(9888);
                server.AddNode(new AssetServer());
                server.AddNode(new LoginServer());
                server.AddNode(new LevelServer());
                server.AddNode(new UserServer());

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

                    Authorizer.LoadPermissions();
                    Users users = new Users();
                    User? user = users.CreateUser("root", password);

                    if (user != null)
                    {
                        // Enable standard root features
                        Authorizer.SetPermission(user, "USER_SVR", "SET_PERMISSION", Authorizer.PermissionAttribute.Allow);
                        Authorizer.SetPermission(user, "USER_SVR", "CREATE_USER", Authorizer.PermissionAttribute.Allow);
                        Authorizer.SetPermission(user, "USER_SVR", "DELETE_USER", Authorizer.PermissionAttribute.Allow);
                    }
                }
            }
        }
    }
}
