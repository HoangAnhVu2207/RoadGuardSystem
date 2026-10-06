namespace RoadGuardSystem.Repositories.Inspections;

// Only finite, recognized business admission rejections cross the caller-owned transaction seam.
// Storage, SQL and cancellation failures are not acknowledgements and remain exceptions.
public sealed class FieldCoreRejectedException(FieldCoreOutcome result) : Exception("FIELD core admission rejected.")
{
    public FieldCoreOutcome Result { get; } = result;
}
