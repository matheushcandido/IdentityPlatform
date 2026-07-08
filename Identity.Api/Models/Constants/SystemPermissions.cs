using System.Collections.Generic;

namespace Identity.Domain.Models.Constants
{
    public static class SystemPermissions
    {
        public static readonly Dictionary<string, string> All =
        new()
        {
            { "Users.Create", "Create users" },
            { "Users.Read", "Read users" },
            { "Users.Update", "Update users" },
            { "Users.Disable", "Disable users" },

            { "Roles.Create", "Create roles" },
            { "Roles.Read", "Read roles" },
            { "Roles.Update", "Update roles" },
            { "Roles.Delete", "Delete roles" },

            { "Permissions.Create", "Create permissions" },
            { "Permissions.Read", "Read permissions" },
            { "Permissions.Update", "Update permissions" },
            { "Permissions.Delete", "Delete permissions" },

            { "Assignments.Grant", "Grant roles and permissions" },
            { "Assignments.Revoke", "Revoke roles and permissions" }
        };
    }
}