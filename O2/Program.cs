using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using O2Client;

namespace O2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "asset":
                        AssetCommand(args);
                        break;
                    case "login":
                        LoginCommand();
                        break;
                    case "create":
                        CreateCommand(args);
                        break;
                    default:
                        Console.WriteLine("Invalid command");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Help");
                Console.WriteLine("o2 login                             Tests login");
                Console.WriteLine("o2 create api-key                    Create an api key used to login");
                Console.WriteLine("o2 asset patch                       Downloads the lastest version of each asset");
                Console.WriteLine("o2 asset upload myAsset.png          Uploads an asset of the file specified");
                Console.WriteLine("o2 asset download myAsset.png        Downloads an asset of the file specified");
                Console.WriteLine("o2 asset list                        Lists the assets stored on the server");
                Console.WriteLine("o2 asset history myAsset.png         Lists the revision history of the asset specified");
                Console.WriteLine("o2 asset restore myAsset.png 10      Restores the specified asset at the revision specified");
            }
        }

        static Client StartClient()
        {
            Client cli = new Client("localhost", 9888);
            return cli;
        }

        //static Client StartThreadedClient()
        //{
        //    Client cli = new Client("localhost", 9888);
        //    cli.RunClientThread();
        //    return cli;
        //}

        static Client? LoginCommand()
        {
            Console.Write("Username: ");
            string? username = Console.ReadLine();
            if (username != null)
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

                if (hashedBytes != null)
                {
                    Client cli = StartClient();

                    try
                    {
                        cli.Login(username, hashedBytes);
                        Console.WriteLine("Logged in successfully");
                        return cli;
                    }
                    catch (ClientException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return null;
        }

        static void CreateCommand(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "api-key":
                        CreateAPIKeyCommand();
                        break;
                }
            }
        }

        static void CreateAPIKeyCommand()
        {
            if (File.Exists("api-key"))
            {
                Console.WriteLine("Error api key file already present");
                return;
            }

            Client? client = LoginCommand();
            if (client != null)
            {
                try
                {
                    string apiKey = client.CreateAPIKey();

                    using (FileStream stream = File.OpenWrite("api-key"))
                    {
                        stream.Write(Encoding.UTF8.GetBytes(apiKey));
                    }
                    Console.WriteLine("API key generated");
                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static string? LoadAPIKey()
        {
            if (File.Exists("api-key"))
            {
                byte[] key = File.ReadAllBytes("api-key");
                return Encoding.UTF8.GetString(key);
            }

            return null;
        }

        private static void LoginWithAPIKey(Client cli)
        {
            string? apikey = LoadAPIKey();
            if (apikey != null)
            {
                cli.Login(apikey);
            }
        }

        static void AssetCommand(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "upload":
                        UploadAssetCommand(args);
                        break;
                    case "download":
                        DownloadAssetCommand(args);
                        break;
                    case "list":
                        ListAssetCommand(args);
                        break;
                    case "history":
                        ListAssetHistoryCommand(args);
                        break;
                    case "patch":
                        PatchAssetsCommand();
                        break;
                    case "restore":
                        RestoreAssetCommand(args);
                        break;
                    default:
                        Console.WriteLine("Invalid command");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        static void RestoreAssetCommand(string[] args)
        {
            if (args.Length > 3)
            {
                string name = args[2];
                int revision = int.Parse(args[3]);

                var client = StartClient();

                try
                {
                    LoginWithAPIKey(client);
                    client.RestoreAsset(name, revision);
                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        static void PatchAssetsCommand()
        {
            Client cli = StartClient();

            try
            {
                LoginWithAPIKey(cli);

                List<string> assets = cli.ListAssets();

                foreach (string asset in assets)
                {
                    cli.DownloadAsset(asset);
                    Console.WriteLine($"Patched {asset}");
                }
            }
            catch (ClientException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void ListAssetHistoryCommand(string[] args)
        {
            if (args.Length > 2)
            {
                string name = args[2];

                var client = StartClient();

                try
                {
                    LoginWithAPIKey(client);
                    Console.WriteLine(client.GetAssetHistory(name));
                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        static void ListAssetCommand(string[] args)
        {
            Client cli = StartClient();

            try
            {
                LoginWithAPIKey(cli);

                List<string> assets = cli.ListAssets();
                foreach (var asset in assets)
                {
                    Console.WriteLine(asset);
                }
            }
            catch (ClientException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Subscriber_Loaded(object? sender, EventArgs e)
        {
            AssetListSubscriber list = (AssetListSubscriber)sender;
            foreach (var asset in list.Assets)
            {
                Console.WriteLine(asset);
            }
        }

        private static void Subscriber_Error(object? sender, ClientException e)
        {
            Console.WriteLine(e.Message);
        }

        static void DownloadAssetCommand(string[] args)
        {
            if (args.Length > 2)
            {
                string name = args[2];

                var client = StartClient();

                try
                {
                    LoginWithAPIKey(client);
                    client.DownloadAsset(name);
                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        static void UploadAssetCommand(string[] args)
        {
            if (args.Length > 2)
            {
                string name = args[2];

                var client = StartClient();

                try
                {
                    LoginWithAPIKey(client);
                    client.UploadAsset(name);
                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }
    }
}
