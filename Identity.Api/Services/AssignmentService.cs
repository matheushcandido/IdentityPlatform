using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services
{
    public class AssignmentService
    {
        private readonly ApplicationDbContext _db;

        public AssignmentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool?> AssignRoleToUserAsync(Guid userId, Guid roleId)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
            var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId);

            if (!userExists || !roleExists)
                return null;

            var alreadyAssigned = await _db.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (alreadyAssigned)
                return false;

            _db.UserRoles.Add(new Identity.Domain.Entities.UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool?> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
        {
            var userRole = await _db.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole is null)
                return null;

            _db.UserRoles.Remove(userRole);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool?> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
        {
            var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId);
            var permissionExists = await _db.Permissions.AnyAsync(p => p.Id == permissionId);

            if (!roleExists || !permissionExists)
                return null;

            var alreadyAssigned = await _db.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (alreadyAssigned)
                return false;

            _db.RolePermissions.Add(new Identity.Domain.Entities.RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool?> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
        {
            var rolePermission = await _db.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission is null)
                return null;

            _db.RolePermissions.Remove(rolePermission);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}