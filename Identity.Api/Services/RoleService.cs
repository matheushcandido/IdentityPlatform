using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public class RoleService
    {
        private readonly ApplicationDbContext _db;

        public RoleService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            return await _db.Roles
                .AsNoTracking()
                .OrderBy(role => role.Name)
                .ToListAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(Guid id)
        {
            return await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(role => role.Id == id);
        }

        public async Task<Role> CreateRoleAsync(string name)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = name };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            return role;
        }

        public async Task<Role?> UpdateRoleAsync(Guid id, string newName)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);

            if (role is null)
                return null;

            role.Name = newName;
            await _db.SaveChangesAsync();
            return role;
        }
    }
}