﻿using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Oxygen;

namespace O2
{
    internal class Program
    {
        private static string cacheName = "o2-cache";

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
                    case "api-key":
                        APIKeyCommand(args);
                        break;
                    case "user":
                        UserCommand(args);
                        break;
                    case "group":
                        GroupCommand(args);
                        break;
                    case "set-permission":
                        SetPermissionCommand(args);
                        break;
                    case "get-permission":
                        GetPermissionCommand(args);
                        break;
                    default:
                        Console.WriteLine("Invalid command");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Help");
                Console.WriteLine("o2 login                                                         Tests login");
                Console.WriteLine("o2 api-key create                                                Create an api key used to login");
                Console.WriteLine("o2 api-key revoke <username>                                     Revokes all api keys for the specified user");
                Console.WriteLine("o2 user create                                                   Create a new user");
                Console.WriteLine("o2 user delete <username>                                        Deletes a user");
                Console.WriteLine("o2 user list                                                     Lists the users in the system");
                Console.WriteLine("o2 user reset                                                    Reset the users password");
                Console.WriteLine("o2 group add <group> <username>                                  Adds a user to a user group");
                Console.WriteLine("o2 asset patch                                                   Downloads the latest version of each asset");
                Console.WriteLine("o2 asset upload <myAsset.png>                                    Uploads an asset of the file specified");
                Console.WriteLine("o2 asset download <myAsset.png>                                  Downloads an asset of the file specified");
                Console.WriteLine("o2 asset list                                                    Lists the assets stored on the server");
                Console.WriteLine("o2 asset history <myAsset.png>                                   Lists the revision history of the asset specified");
                Console.WriteLine("o2 asset restore <myAsset.png> <10>                              Restores the specified asset at the revision specified");
                Console.WriteLine("o2 set-permission <user> <LOGIN_SVR> <CREATE_API_KEY> <allow>    Sets a permission for a particular operation");
                Console.WriteLine("                                                                 Options: Allow/Deny/Default");
                Console.WriteLine("o2 get-permission <user>                                         Gets the permissions for the specified user");
            }
        }

        static ClientConnection StartClient()
        {
            string hostname = Environment.GetEnvironmentVariable("OXYGEN_HOST") ?? "localhost";
            if (!int.TryParse(Environment.GetEnvironmentVariable("OXYGEN_PORT"), out int port))
            {
                port = 9888;
            }

            ClientConnection cli = new ClientConnection(hostname, port);
            return cli;
        }

        static string? UserNamePrompt()
        {
            Console.Write("Username: ");
            return Console.ReadLine();
        }

        static byte[] PasswordPrompt()
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

        static ClientConnection? LoginCommand()
        {
            string? username = UserNamePrompt();
            if (username != null)
            {
                byte[] hashedBytes = PasswordPrompt();

                if (hashedBytes != null)
                {
                    ClientConnection cli = StartClient();

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

        static void ChangePassword()
        {
            ClientConnection cli = StartClient();

            try
            {
                LoginWithAPIKey(cli);

                Console.WriteLine("Change password");
                byte[] password = PasswordPrompt();
                byte[] newpassword = PasswordPrompt();
                cli.ResetPassword(password, newpassword);
            }
            catch (ClientException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void APIKeyCommand(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "create":
                        CreateAPIKeyCommand();
                        break;
                    case "revoke":
                        RevokeAPIKeyCommand(args);
                        break;
                }
            }
        }

        static void RevokeAPIKeyCommand(string[] args)
        {
            if (args.Length == 3)
            {
                string username = args[2];

                var client = StartClient();

                try
                {
                    LoginWithAPIKey(client);
                    client.RevokeAPIKeys(username);
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

        static void CreateAPIKeyCommand()
        {
            if (File.Exists("api-key"))
            {
                Console.WriteLine("Error api key file already present");
                return;
            }

            ClientConnection? client = LoginCommand();
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

        static void GetPermissionCommand(string[] args)
        {
            if (args.Length == 2)
            {
                string username = args[1];

                var client = StartClient();

                try
                {
                    LoginWithAPIKey(client);
                    foreach (var permission in client.GetPermissionsForUser(username))
                    {
                        Console.WriteLine(permission);
                    }
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

        static void SetPermissionCommand(string[] args)
        {
            if (args.Length == 5)
            {
                string username = args[1];
                string nodeName = args[2];
                string messageName = args[3];
                string permission = args[4].ToLower();

                if (permission == "allow" || permission == "deny" || permission == "default")
                {
                    var client = StartClient();

                    try
                    {
                        LoginWithAPIKey(client);
                        client.SetPermission(username, nodeName, messageName, permission);
                    }
                    catch (ClientException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Permission should be 'allow' or 'deny'");
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        static void CreateNewUserCommand()
        {
            Console.WriteLine("Create New User");
            string? username = UserNamePrompt();
            if (username != null)
            {
                byte[] password = PasswordPrompt();
                if (password != null)
                {
                    var cli = StartClient();
                    try
                    {
                        LoginWithAPIKey(cli);
                        cli.CreateNewUser(username, password);
                    }
                    catch (ClientException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        static void GroupCommand(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "add":
                        AddUserToGroupCommand(args);
                        break;
                }
            }
        }

        static void AddUserToGroupCommand(string[] args)
        {
            if (args.Length > 3)
            {
                string group = args[2];
                string username = args[3];

                ClientConnection cli = StartClient();

                try
                {
                    LoginWithAPIKey(cli);
                    cli.AddUserToGroup(username, group);
                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static void UserCommand(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "list":
                        ListUserCommand();
                        break;
                    case "create":
                        CreateNewUserCommand();
                        break;
                    case "delete":
                        DeleteUserCommand(args);
                        break;
                    case "reset":
                        ChangePassword();
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        static void DeleteUserCommand(string[] args)
        {
            if (args.Length == 3)
            {
                string user = args[2];

                ClientConnection cli = StartClient();

                try
                {
                    LoginWithAPIKey(cli);
                    cli.DeleteUser(user);
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

        static void ListUserCommand()
        {
            ClientConnection cli = StartClient();

            try
            {
                LoginWithAPIKey(cli);
                foreach (string user in cli.ListUsers())
                {
                    Console.WriteLine(user);
                }
            }
            catch (ClientException ex)
            {
                Console.WriteLine(ex.Message);
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
            if (args.Length == 4)
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
            ClientConnection cli = StartClient();

            try
            {
                LoginWithAPIKey(cli);
                cli.LoadCache(cacheName);

                IList<string> assets = cli.ListAssets();

                foreach (string asset in assets)
                {
                    cli.DownloadAsset(asset);
                    Console.WriteLine($"Patched {asset}");
                }
                cli.SaveCache();
            }
            catch (ClientException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void ListAssetHistoryCommand(string[] args)
        {
            if (args.Length == 3)
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
            ClientConnection cli = StartClient();

            try
            {
                LoginWithAPIKey(cli);

                IList<string> assets = cli.ListAssets();
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

        static void DownloadAssetCommand(string[] args)
        {
            if (args.Length == 3)
            {
                string name = args[2];

                var client = StartClient();

                try
                {
                    client.LoadCache(cacheName);
                    LoginWithAPIKey(client);
                    client.DownloadAsset(name);
                    client.SaveCache();
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
            if (args.Length == 3)
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
