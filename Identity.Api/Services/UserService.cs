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

        public async Task<List<User>> GetUsersAsync()
        {
            return await _db.Users
                .AsNoTracking()
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                        .ThenInclude(role => role.RolePermissions)
                            .ThenInclude(rolePermission => rolePermission.Permission)
                .OrderBy(user => user.Email)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _db.Users
                .AsNoTracking()
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                        .ThenInclude(role => role.RolePermissions)
                            .ThenInclude(rolePermission => rolePermission.Permission)
                .FirstOrDefaultAsync(user => user.Id == id);
        }

        public async Task<UserMutationResult> UpdateUserEmailAsync(Guid id, string email)
        {
            var normalizedEmail = NormalizeEmail(email);
            var emailAlreadyExists = await _db.Users.AnyAsync(user =>
                user.Id != id && user.Email == normalizedEmail);

            if (emailAlreadyExists)
            {
                return UserMutationResult.EmailAlreadyExists();
            }

            var user = await _db.Users.FirstOrDefaultAsync(user => user.Id == id);
            if (user == null)
            {
                return UserMutationResult.NotFound();
            }

            user.Email = normalizedEmail;
            await _db.SaveChangesAsync();

            return UserMutationResult.Success(await GetUserByIdAsync(id));
        }

        public async Task<UserMutationResult> DisableUserAsync(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(user => user.Id == id);
            if (user == null)
            {
                return UserMutationResult.NotFound();
            }

            user.IsActive = false;
            await _db.SaveChangesAsync();

            return UserMutationResult.Success(await GetUserByIdAsync(id));
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }

    public sealed record UserMutationResult(UserMutationStatus Status, User? User)
    {
        public static UserMutationResult Success(User? user)
        {
            return new UserMutationResult(UserMutationStatus.Success, user);
        }

        public static UserMutationResult NotFound()
        {
            return new UserMutationResult(UserMutationStatus.NotFound, null);
        }

        public static UserMutationResult EmailAlreadyExists()
        {
            return new UserMutationResult(UserMutationStatus.EmailAlreadyExists, null);
        }
    }

    public enum UserMutationStatus
    {
        Success,
        NotFound,
        EmailAlreadyExists
    }
}
