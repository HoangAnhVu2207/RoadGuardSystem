using System;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Identity;

/// <summary>
/// Represents a canonical system role per Option A / Data Dictionary section 3.1.
/// In C# domain code, keyed by UserRoleCode enum.
/// In EF Core persistence, mapped to canonical VARCHAR(40) string PK (SUPERVISOR, PM, DRONE_OPERATOR, REPAIR_CREW).
/// </summary>
public class ApplicationRole
{
    public UserRoleCode Code { get; set; } = UserRoleCode.Unknown;

    public string Name { get; set; } = string.Empty;

    public string? NormalizedName { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationRole()
    {
    }

    public ApplicationRole(UserRoleCode code, string name)
    {
        Code = code;
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        IsActive = true;
    }

    public ApplicationRole(string codeStr, string name)
        : this(UserRoleCodeExtensions.FromDbCode(codeStr), name)
    {
    }
}
