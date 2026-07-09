using Identity.Domain.Entities;
using Identity.Domain.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services;

public static class PermissionSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        foreach (var permissionDefinition in SystemPermissions.All)
        {
            var exists = await db.Permissions
                .AnyAsync(p => p.Name == permissionDefinition.Key);

            if (!exists)
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = permissionDefinition.Key,
                    Description = permissionDefinition.Value
                });
            }
        }

        await db.SaveChangesAsync();
    }
}