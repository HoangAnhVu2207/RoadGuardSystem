using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Authorization;

internal static class ProjectRoleClaimParser
{
    internal static bool TryParse(string? value, out UserRoleCode role)
    {
        try
        {
            role = UserRoleCodeExtensions.FromDbCode(value ?? string.Empty);
            return role != UserRoleCode.Unknown;
        }
        catch (ArgumentOutOfRangeException)
        {
            role = UserRoleCode.Unknown;
            return false;
        }
    }
}
