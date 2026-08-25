using Identity.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Identity.Api.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher _hasher;
        private readonly IDataProtector _totpProtector;

        public AuthService(ApplicationDbContext db, PasswordHasher hasher, IDataProtectionProvider dataProtectionProvider)
        {
            _db = db;
            _hasher = hasher;
            _totpProtector = dataProtectionProvider.CreateProtector("IdentityPlatform.MfaTotpSecret.v1");
        }

        public async Task<User?> ValidateUser(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

            if (user == null)
                return null;

            if (!_hasher.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public Task<User?> GetActiveUserAsync(Guid id)
        {
            return _db.Users.FirstOrDefaultAsync(user => user.Id == id && user.IsActive);
        }

        public async Task<User?> GetUserWithAccessAsync(Guid id)
        {
            return await _db.Users
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                        .ThenInclude(role => role.RolePermissions)
                            .ThenInclude(rolePermission => rolePermission.Permission)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public string? GetTotpSecret(User user)
        {
            if (string.IsNullOrEmpty(user.TotpSecret)) return null;
            try { return _totpProtector.Unprotect(user.TotpSecret); }
            catch (CryptographicException) { return null; }
        }

        public void SetTotpSecret(User user, string secret) => user.TotpSecret = _totpProtector.Protect(secret);
    }
}
