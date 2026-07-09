using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public static class BootstrapAdminSeeder
    {
        private const string AdminRoleName = "Admin";

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<PasswordHasher>();

            var email = configuration["BootstrapAdmin:Email"];
            var password = configuration["BootstrapAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("BootstrapAdmin configuration is required.");
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var admin = await db.Users
                .FirstOrDefaultAsync(user => user.Email == normalizedEmail);

            if (admin == null)
            {
                admin = new User
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    PasswordHash = hasher.Hash(password),
                    IsActive = true
                };

                db.Users.Add(admin);
            }

            var adminRole = await db.Roles
                .FirstOrDefaultAsync(role => role.Name == AdminRoleName);

            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = AdminRoleName
                };

                db.Roles.Add(adminRole);
            }

            await db.SaveChangesAsync();

            var permissions = await db.Permissions.ToListAsync();

            var adminHasRole = await db.UserRoles.AnyAsync(userRole =>
                userRole.UserId == admin.Id &&
                userRole.RoleId == adminRole.Id);

            if (!adminHasRole)
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    RoleId = adminRole.Id
                });
            }

            foreach (var permission in permissions)
            {
                var roleHasPermission = await db.RolePermissions.AnyAsync(rolePermission =>
                    rolePermission.RoleId == adminRole.Id &&
                    rolePermission.PermissionId == permission.Id);

                if (!roleHasPermission)
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}