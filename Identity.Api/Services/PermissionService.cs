using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public class PermissionService
    {
        private readonly ApplicationDbContext _db;

        public PermissionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Permission>> GetPermissionsAsync()
        {
            return await _db.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Permission?> GetPermissionByIdAsync(Guid id)
        {
            return await _db.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Permission?> CreatePermissionAsync(string name, string description, PermissionType permissionType)
        {
            var exists = await _db.Permissions.AnyAsync(p => p.Name == name);
            if (exists) return null;

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Type = permissionType
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();
            return permission;
        }

        public async Task<Permission?> UpdatePermissionAsync(Guid id, string name, string description)
        {
            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
            if (permission is null) return null;

            var nameConflict = await _db.Permissions.AnyAsync(p => p.Name == name && p.Id != id);
            if (nameConflict) return null;

            permission.Name = name;
            permission.Description = description;
            await _db.SaveChangesAsync();
            return permission;
        }

        public async Task<bool> DeletePermissionAsync(Guid id)
        {
            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
            if (permission is null) return false;

            _db.Permissions.Remove(permission);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}