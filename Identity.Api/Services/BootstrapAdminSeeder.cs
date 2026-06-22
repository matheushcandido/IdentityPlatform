using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public static class BootstrapAdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<PasswordHasher>();

            if (await db.Users.AnyAsync())
            {
                return;
            }

            var email = configuration["BootstrapAdmin:Email"];
            var password = configuration["BootstrapAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin configuration is required when the database has no users.");
            }

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = hasher.Hash(password),
                IsActive = true
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
