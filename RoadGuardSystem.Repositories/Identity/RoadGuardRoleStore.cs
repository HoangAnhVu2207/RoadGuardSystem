using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed class RoadGuardRoleStore : IRoleStore<ApplicationRole>, IQueryableRoleStore<ApplicationRole>
{
    private readonly RoadGuardDbContext _context;

    public RoadGuardRoleStore(RoadGuardDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IQueryable<ApplicationRole> Roles => _context.Roles;

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.Code.ToDbCode());
    }

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult<string?>(role.Name);
    }

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Name = roleName ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.NormalizedName);
    }

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        try
        {
            var code = UserRoleCodeExtensions.FromDbCode(roleId);
            return _context.Roles.SingleOrDefaultAsync(r => r.Code == code, cancellationToken);
        }
        catch
        {
            return Task.FromResult<ApplicationRole?>(null);
        }
    }

    public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedRoleName))
        {
            return Task.FromResult<ApplicationRole?>(null);
        }

        var normalized = normalizedRoleName.Trim().ToUpperInvariant();
        try
        {
            var code = UserRoleCodeExtensions.FromDbCode(normalized);
            return _context.Roles.SingleOrDefaultAsync(
                r => r.Code == code || r.NormalizedName == normalized,
                cancellationToken);
        }
        catch
        {
            return _context.Roles.SingleOrDefaultAsync(
                r => r.NormalizedName == normalized,
                cancellationToken);
        }
    }

    public void Dispose()
    {
    }
}
