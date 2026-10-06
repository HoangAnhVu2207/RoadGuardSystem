namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository : IIdentityRepository, IIdentityV2Repository
{
    private readonly RoadGuardDbContext _context;
    private readonly TimeProvider _timeProvider;

    public IdentityRepository(RoadGuardDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }
}
