using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher _hasher;

        public AuthService(ApplicationDbContext db, PasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<User?> ValidateUser(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
                return null;

            if (!_hasher.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _db.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }
    }
}
