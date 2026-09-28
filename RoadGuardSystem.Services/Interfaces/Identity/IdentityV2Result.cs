namespace RoadGuardSystem.Services.Identity;

public sealed record IdentityV2Result(
    IdentityV2Status Status,
    IdentityV2Actor? Actor = null);
