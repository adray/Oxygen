using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    public enum PermissionAttribute
    {
        Allow = 0,
        Deny = 1,
        Default = 2
    }

    public class Permission
    {
        public string NodeName { get; set; } = string.Empty;
        public string MessageName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public PermissionAttribute Attribute { get; set; }
    }

    public enum PermissionInherit
    {
        Group = 0,
        User = 1,
        Default = 2
    }

    public class PermissionItem
    {
        public string NodeName { get; set; } = string.Empty;
        public string MessageName { get; set; } = string.Empty;
        public PermissionAttribute Attribute { get; set; }
        public PermissionInherit Inherit { get; set; }
    }
}
