﻿using System;
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
            Authorizer.SetPermission(demo, "ASSET_SVR", "ASSET_LIST", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "LOAD_LEVEL", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "CLOSE_LEVEL", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "LIST_LEVELS", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "ADD_OBJECT", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "UPDATE_OBJECT", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "OBJECT_STREAM", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "EVENT_STREAM", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(demo, "LEVEL_SVR", "UPDATE_CURSOR", Authorizer.PermissionAttribute.Allow);
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
            Authorizer.SetPermission(admins, "USER_SVR", "SET_PERMISSION", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "CREATE_USER", Authorizer.PermissionAttribute.Allow);
            Authorizer.SetPermission(admins, "USER_SVR", "DELETE_USER", Authorizer.PermissionAttribute.Allow);
        }
    }
}
