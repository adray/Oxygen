using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal static class Setup
    {
        public static void Install()
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
            CreateAdmin(users, password);
            CreateDemo(users);
        }

        private static void CreateDemo(Users users)
        {
            UserGroup? demo = users.CreateUserGroup("demo");
            if (demo == null)
            {
                Console.WriteLine("Fatal error: unable to create demo user group");
                return;
            }

            // Enable demo features
            Authorizer.SetPermission(demo, "ASSET_SVR", "ASSET_LIST", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "LOAD_LEVEL", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "CLOSE_LEVEL", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "LIST_LEVELS", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "ADD_OBJECT", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "UPDATE_OBJECT", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "OBJECT_STREAM", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "EVENT_STREAM", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "UPDATE_CURSOR", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "METRIC_SVR", "REPORT_METRIC", PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "METRIC_SVR", "METRIC_COLLECTION", PermissionAttribute.Allow);
		}

        private static void CreateAdmin(Users users, string password)
        {
            UserGroup? admins = users.CreateUserGroup("admins");
            if (admins == null)
            {
                Console.WriteLine("Fatal error: unable to create admin user group");
                return;
            }

            User? user = users.CreateUser("root", password);
            if (user == null)
            {
                Console.WriteLine("Fatal error: unable to create root user");
                return;
            }

            users.AddUserToGroup(user.Name, admins.Name);

            // Enable standard root features
            Authorizer.SetPermission(admins, "USER_SVR", "GET_PERMISSION", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "SET_PERMISSION", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "GET_MY_PERMISSION", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "GET_ALL_PERMISSION", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "CREATE_API_KEY", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "CREATE_USER", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "DELETE_USER", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "USER_LIST", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "CREATE_USER_GROUP", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "ADD_USER_TO_GROUP", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "REMOVE_USER_FROM_GROUP", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "USER_GROUP_INFO", PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "USER_GROUP_LIST", PermissionAttribute.Allow);
        }
    }
}
