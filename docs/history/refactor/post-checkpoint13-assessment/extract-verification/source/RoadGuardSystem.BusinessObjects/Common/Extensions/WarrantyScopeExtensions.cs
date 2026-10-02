namespace RoadGuardSystem.aBusinessObjects.Commons;

public static class WarrantyScopeExtensions
{
    public static string ToApiCode(this WarrantyScope scope) => scope switch
    {
        WarrantyScope.Project => "PROJECT",
        WarrantyScope.RoadSection => "ROAD_SECTION",
        WarrantyScope.ContractItem => "CONTRACT_ITEM",
        WarrantyScope.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown warranty scope.")
    };
}
