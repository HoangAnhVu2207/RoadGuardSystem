using System;

namespace RoadGuardSystem.aBusinessObjects.Commons;

public static class UserRoleCodeExtensions
{
    public const string SupervisorDbCode = "SUPERVISOR";
    public const string ProjectManagerDbCode = "PM";
    public const string DroneOperatorDbCode = "DRONE_OPERATOR";
    public const string RepairCrewDbCode = "REPAIR_CREW";

    public static string ToDbCode(this UserRoleCode role)
    {
        return role switch
        {
            UserRoleCode.Supervisor => SupervisorDbCode,
            UserRoleCode.ProjectManager => ProjectManagerDbCode,
            UserRoleCode.DroneOperator => DroneOperatorDbCode,
            UserRoleCode.RepairCrew => RepairCrewDbCode,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, $"Unknown or invalid UserRoleCode '{role}' cannot be mapped to a canonical database role code.")
        };
    }

    public static UserRoleCode FromDbCode(string? dbCode)
    {
        if (string.IsNullOrWhiteSpace(dbCode))
        {
            throw new ArgumentOutOfRangeException(nameof(dbCode), dbCode, "Role database code cannot be null or empty.");
        }

        return dbCode.Trim() switch
        {
            SupervisorDbCode => UserRoleCode.Supervisor,
            ProjectManagerDbCode => UserRoleCode.ProjectManager,
            DroneOperatorDbCode => UserRoleCode.DroneOperator,
            RepairCrewDbCode => UserRoleCode.RepairCrew,
            _ => throw new ArgumentOutOfRangeException(nameof(dbCode), dbCode, $"Unsupported database role code '{dbCode}'.")
        };
    }
}
