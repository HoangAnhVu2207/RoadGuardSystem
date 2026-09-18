namespace RoadGuardSystem.BusinessObjects.Concurrency;

public interface IHasRowVersion
{
    byte[] RowVersion { get; }
}
