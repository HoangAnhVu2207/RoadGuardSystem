namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository : IIdentityRepository, IIdentityV2Repository
{
    private readonly RoadGuardDbContext _context;

    public IdentityRepository(RoadGuardDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
