using Identity.Domain.Entities;
using Identity.Domain.Enums;
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

        public async Task<Role> CreateRoleAsync(string name, string description, RoleType roleType)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = name, Description = description, Type = roleType };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            return role;
        }

        public async Task<Role?> UpdateRoleAsync(Guid id, string newName, string newDescription)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);

            if (role is null)
                return null;

            role.Name = newName;
            role.Description = newDescription;
            await _db.SaveChangesAsync();
            return role;
        }
    }
}