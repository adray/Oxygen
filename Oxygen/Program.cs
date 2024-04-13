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
                server.AddNode(new MetricsServer());

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
                    Setup.Install();
                }
            }
        }
    }
}
