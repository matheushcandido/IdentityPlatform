using Identity.Domain.Entities;

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

        public async Task CreateUser(string email, string password)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = _hasher.Hash(password),
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
    }
}
