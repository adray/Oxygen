using System.Diagnostics;
using System.Globalization;
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
        private static readonly string directory = ".o2";
        private static readonly string cacheName = @".o2\cache";

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Commands commands = new Commands();
                commands.AddCommand(new DownloadArtefact(), "build", "download");
                commands.AddCommand(new UploadArtefact(), "build", "upload");
                commands.AddCommand(new ListArtefact(), "build", "list");
                commands.AddCommand(new PluginList(), "plugin", "list");
                commands.AddCommand(new PluginSchedule(), "plugin", "schedule");
                commands.AddCommand(new SearchTag(), "tag", "search");
                commands.AddCommand(new AddTag(), "tag", "add");
                commands.AddCommand(new TagsForAsset(), "tag", "get");
                commands.AddCommand(new DownloadAsset(), "asset", "download");
                commands.AddCommand(new UploadAsset(), "asset", "upload");
                commands.AddCommand(new PatchAssets(), "asset", "patch");
                commands.AddCommand(new ListAssetHistory(), "asset", "history");
                commands.AddCommand(new ListAsset(), "asset", "list");
                commands.AddCommand(new RestoreAsset(), "asset", "restore");
                commands.AddCommand(new CreateUser(), "user", "create");
                commands.AddCommand(new DeleteUser(), "user", "delete");
                commands.AddCommand(new ListUsers(), "user", "list");
                commands.AddCommand(new ResetUser(), "user", "reset");
                commands.AddCommand(new LabelList(), "label", "list");
                commands.AddCommand(new LabelSpec(), "label", "spec");
                commands.AddCommand(new AddUserToGroup(), "group", "add");
                commands.AddCommand(new RemoveUserFromGroup(), "group", "remove");
                commands.AddCommand(new CreateUserGroupCommand(), "group", "create");
                commands.AddCommand(new DeleteUserGroupCommand(), "group", "delete");
                commands.AddCommand(new ListUserGroupsCommand(), "group", "list");
                commands.AddCommand(new ListUsersInGroupCommand(), "group", "info");
                commands.AddCommand(new SetPermission(), "set-permission");
                commands.AddCommand(new GetPermission(), "get-permission");
                commands.AddCommand(new SetGroupPermission(), "set-group-permission");
                commands.AddCommand(new CreateAPIKey(), "api-key", "create");
                commands.AddCommand(new RevokeAPIKey(), "api-key", "revoke");
                commands.AddCommand(new Login(), "login");
                commands.AddCommand(new Init(), "init");

                if (!commands.Invoke(args))
                {
                    Console.WriteLine("Invalid command");
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
                Console.WriteLine("o2 group create <group>                                          Create a new user group");
                Console.WriteLine("o2 group delete <group>                                          Deletes a user group");
                Console.WriteLine("o2 group add <group> <username>                                  Adds a user to a user group");
                Console.WriteLine("o2 group remove <group> <username>                               Remove a user from a user group");
                Console.WriteLine("o2 group list                                                    Lists the groups");
                Console.WriteLine("o2 group info <group>                                            Lists the users in a user group");
                Console.WriteLine("o2 asset patch                                                   Downloads the latest version of each asset");
                Console.WriteLine("o2 asset upload <myAsset.png>                                    Uploads an asset of the file specified");
                Console.WriteLine("o2 asset download <myAsset.png>                                  Downloads an asset of the file specified");
                Console.WriteLine("o2 asset list                                                    Lists the assets stored on the server");
                Console.WriteLine("o2 asset history <myAsset.png>                                   Lists the revision history of the asset specified");
                Console.WriteLine("o2 asset restore <myAsset.png> <10>                              Restores the specified asset at the revision specified");
                Console.WriteLine("o2 set-permission <user> <LOGIN_SVR> <CREATE_API_KEY> <allow>    Sets a permission for a particular operation");
                Console.WriteLine("                                                                 Options: Allow/Deny/Default");
                Console.WriteLine("o2 get-permission <user>                                         Gets the permissions for the specified user");
                Console.WriteLine("o2 set-group-permission <group> <LOGIN_SVR> <CREATE_API_KEY> <allow>  Sets the permissions for the specified user group");
                Console.WriteLine("                                                                      Options: Allow/Deny/Default");
                Console.WriteLine("o2 label create <name>                                           Creates a label of the head versions.");
                Console.WriteLine("o2 label spec <name>                                             Gets the label spec of the specified label.");
                Console.WriteLine("o2 label list                                                    Gets the list of labels.");
                Console.WriteLine("o2 tag search <tag>                                              Searches for assets matching the specified tag.");
                Console.WriteLine("o2 tag add <asset> <tag>                                         Adds a tag for the specified asset.");
                Console.WriteLine("                                                                 This can be a comma seperated list of tags.");
                Console.WriteLine("o2 tag get <asset>                                               Gets the tags for the specified asset.");
                Console.WriteLine("o2 plugin list                                                   Gets the installed plugins on the server.");
                Console.WriteLine("o2 plugin schedule                                               Gets the schedule for plugins operating on the server.");
                Console.WriteLine("o2 build upload <name>                                           Uploads a build artefact to the server.");
                Console.WriteLine("o2 build download <name>                                         Downloads a build artefact from the server.");
                Console.WriteLine("o2 build list                                                    Lists the build artefacts on the server.");
                //Console.WriteLine("o2 crash upload <name>");
                //Console.WriteLine("o2 crash download <name>");
            }
        }

        private static void AddOrRemoveUserGroup(ClientConnection client, string[] args, bool addOrRemove)
        {
            if (args.Length > 3)
            {
                string group = args[2];
                string username = args[3];

                if (addOrRemove)
                {
                    client.AddUserToGroup(username, group);
                }
                else
                {
                    client.RemoveUserFromGroup(username, group);
                }
            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }

        class Init : Command
        {
            public override void Invoke(string[] args)
            {
                base.Invoke(args);

                if (Directory.Exists(directory))
                {
                    Console.WriteLine("Already initialized");
                }
                else
                {
                    Directory.CreateDirectory(directory);
                    File.SetAttributes(directory, FileAttributes.Hidden);
                    Console.WriteLine("Initialiazed");
                }
            }
        }

        class Login : Command
        {
            public override void Invoke(string[] args)
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
                        }
                        catch (ClientException ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }

        class RevokeAPIKey : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string username = args[2];

                    client.RevokeAPIKeys(username);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class CreateAPIKey : Command
        {
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

            public override void Invoke(string[] args)
            {
                if (File.Exists("api-key"))
                {
                    Console.WriteLine("Error api key file already present");
                    return;
                }

                ClientConnection? client = LoginCommand();
                if (client != null)
                {
                    Invoke(client, args);
                }
            }

            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (Directory.Exists(directory))
                {
                    try
                    {
                        string apiKey = client.CreateAPIKey();

                        using (FileStream stream = File.OpenWrite(directory + @"\api-key"))
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
                else
                {
                    Console.WriteLine("Not initialized.");
                }
            }
        }

        class SetGroupPermission : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 5)
                {
                    string group = args[1];
                    string nodeName = args[2];
                    string messageName = args[3];
                    string permission = args[4].ToLower();

                    if (permission == "allow" || permission == "deny" || permission == "default")
                    {
                        client.SetGroupPermission(group, nodeName, messageName, permission);
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
        }

        class GetPermission : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 2)
                {
                    string username = args[1];

                    foreach (var permission in client.GetPermissionsForUser(username))
                    {
                        if (permission.Inherit == PermissionInherit.Group)
                        {
                            Console.WriteLine("{0}.{1} - {2} [Group]", permission.NodeName, permission.MessageName, permission.Attribute);
                        }
                        else if (permission.Inherit == PermissionInherit.User)
                        {
                            Console.WriteLine("{0}.{1} - {2} [User]", permission.NodeName, permission.MessageName, permission.Attribute);
                        }
                        else
                        {
                            Console.WriteLine("{0}.{1} - {2}", permission.NodeName, permission.MessageName, permission.Attribute);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class SetPermission : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 5)
                {
                    string username = args[1];
                    string nodeName = args[2];
                    string messageName = args[3];
                    string permission = args[4].ToLower();

                    if (permission == "allow" || permission == "deny" || permission == "default")
                    {
                        client.SetPermission(username, nodeName, messageName, permission);
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
        }

        class ListUsersInGroupCommand : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length > 2)
                {
                    foreach (var user in client.ListUsersInGroup(args[2]))
                    {
                        Console.WriteLine(user);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class ListUserGroupsCommand : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                var groups = client.ListUserGroups();
                foreach (var group in groups)
                {
                    Console.WriteLine(group);
                }
            }
        }

        class DeleteUserGroupCommand : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length > 2)
                {
                    string group = args[2];

                    client.DeleteUserGroup(group);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class CreateUserGroupCommand : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length > 2)
                {
                    string group = args[2];
                    client.CreateUserGroup(group);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class RemoveUserFromGroup : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                AddOrRemoveUserGroup(client, args, false);
            }
        }

        class AddUserToGroup : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                AddOrRemoveUserGroup(client, args, true);
            }
        }

        class LabelList : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                List<string> list = client.GetAssetLabels();
                foreach (string item in list)
                {
                    Console.WriteLine(item);
                }
            }
        }

        class LabelSpec : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length > 2)
                {
                    List<string> spec = client.GetAssetLabelSpec(args[2]);
                    foreach (string item in spec)
                    {
                        Console.WriteLine(item);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class CreateLabel : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length > 2)
                {
                    client.CreateAssetLabel(args[2]);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class ResetUser : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                Console.WriteLine("Change password");
                byte[] password = PasswordPrompt();
                byte[] newpassword = PasswordPrompt();
                client.ResetPassword(password, newpassword);
            }
        }

        class CreateUser : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                Console.WriteLine("Creating New User");
                string? username = UserNamePrompt();
                if (username != null)
                {
                    byte[] password = PasswordPrompt();
                    if (password != null)
                    {
                        client.CreateNewUser(username, password);
                    }
                }
            }
        }

        class DeleteUser : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string user = args[2];

                    client.DeleteUser(user);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class ListUsers : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                foreach (string user in client.ListUsers())
                {
                    Console.WriteLine(user);
                }
            }
        }

        class RestoreAsset : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 4)
                {
                    string name = args[2];
                    int revision = int.Parse(args[3]);

                    client.RestoreAsset(name, revision);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class PatchAssets : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                client.LoadCache(cacheName);

                IList<string> assets = client.ListAssets();

                foreach (string asset in assets)
                {
                    client.DownloadAsset(asset);
                    Console.WriteLine($"Patched {asset}");
                }
                client.SaveCache();
            }
        }

        class ListAssetHistory : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string name = args[2];

                    Console.WriteLine(client.GetAssetHistory(name));
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class ListAsset : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                IList<string> assets = client.ListAssets();
                foreach (var asset in assets)
                {
                    Console.WriteLine(asset);
                }
            }
        }

        class DownloadAsset : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string name = args[2];

                    client.LoadCache(cacheName);
                    client.DownloadAsset(name);
                    client.SaveCache();
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class UploadAsset : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string name = args[2];

                    client.UploadAsset(name);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class AddTag : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 4)
                {
                    string asset = args[2];
                    string tags = args[3];

                    var tagList = new List<string>(tags.Split(","));
                    client.AddAssetTags(asset, tagList);
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class SearchTag : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string tag = args[2];
                    foreach (var asset in client.SearchAssets("TAG", tag))
                    {
                        Console.WriteLine(asset);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class TagsForAsset : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                if (args.Length == 3)
                {
                    string asset = args[2];
                    foreach (var tag in client.GetTagsForAsset(asset))
                    {
                        Console.WriteLine(tag);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command");
                }
            }
        }

        class PluginList : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                foreach (var plugin in client.GetPlugins())
                {
                    Console.WriteLine(plugin);
                }
            }
        }

        class PluginSchedule : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                var schedule = client.GetSchedule();

                if (schedule.Count == 0)
                {
                    Console.WriteLine("No items scheduled.");
                }
                else
                {
                    Console.WriteLine(string.Empty.PadRight(80, '='));
                    Console.Write("Name".PadRight(20));
                    Console.Write("Started By".PadRight(12));
                    Console.Write("Running".PadRight(10));
                    Console.Write("Start".PadRight(16));
                    Console.Write("Manual".PadRight(10));
                    Console.WriteLine();
                    Console.WriteLine(string.Empty.PadRight(80, '='));
                    foreach (var item in schedule)
                    {
                        Console.Write(item.Name.PadRight(20));
                        Console.Write(item.StartedBy.PadRight(12));

                        if (item.Running)
                        {
                            Console.Write("Yes".PadRight(10));
                        }
                        else
                        {
                            Console.Write("No".PadRight(10));
                        }

                        Console.Write(item.Date.PadRight(16));

                        if (item.StartedManually)
                        {
                            Console.Write("Yes".PadRight(10));
                        }
                        else
                        {
                            Console.Write("No".PadRight(10));
                        }
                        Console.WriteLine();
                    }

                    Console.WriteLine(string.Empty.PadRight(80, '='));
                }
            }
        }

        class DownloadArtefact : Command
        {
            private readonly Stopwatch stopwatch = new Stopwatch();

            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                stopwatch.Start();
                client.DataReceived += Client_DataReceived;
                client.DownloadArtefact(args[2]);
                stopwatch.Stop();
            }

            private void Client_DataReceived(object? sender, DataEventArgs e)
            {
                double totalMegaBytes = Math.Round(e.TotalBytes / 1024.0 / 1024.0, 2);
                double transferredMegaBytes = Math.Round(e.TransferredBytes / 1024.0 / 1024.0, 2);

                double speed = Math.Round(transferredMegaBytes / stopwatch.Elapsed.TotalSeconds, 1);

                Console.CursorLeft = 0;
                Console.Write("{0} {1}/{2}MiB {3:c} {4}MB/s",
                    e.Filename,
                    transferredMegaBytes,
                    totalMegaBytes,
                    stopwatch.Elapsed,
                    speed);
            }
        }

        class UploadArtefact : Command
        {
            private readonly Stopwatch stopwatch = new Stopwatch();

            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                stopwatch.Start();
                client.DataSent += Client_DataSent;
                client.UploadArtefact(args[2]);
                stopwatch.Stop();
            }

            private void Client_DataSent(object? sender, DataEventArgs e)
            {
                double totalMegaBytes = Math.Round(e.TotalBytes / 1024.0 / 1024.0, 2);
                double transferredMegaBytes = Math.Round(e.TransferredBytes / 1024.0 / 1024.0, 2);

                double speed = Math.Round(transferredMegaBytes / stopwatch.Elapsed.TotalSeconds, 1);

                Console.CursorLeft = 0;
                Console.Write("{0} {1}/{2}MiB {3:c}",
                    e.Filename,
                    transferredMegaBytes,
                    totalMegaBytes,
                    stopwatch.Elapsed,
                    speed);
            }
        }

        class ListArtefact : Command
        {
            public override void Invoke(ClientConnection client, string[] args)
            {
                base.Invoke(client, args);

                foreach (string artefact in client.GetArtefacts())
                {
                    Console.WriteLine(artefact);
                }
            }
        }
    }
}
