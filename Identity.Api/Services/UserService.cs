using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher _hasher;

        public UserService(ApplicationDbContext db, PasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<User?> CreateUser(string email, string password)
        {
            var normalizedEmail = NormalizeEmail(email);
            var emailAlreadyExists = await _db.Users.AnyAsync(user => user.Email == normalizedEmail);
            if (emailAlreadyExists)
            {
                return null;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = _hasher.Hash(password),
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return user;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
