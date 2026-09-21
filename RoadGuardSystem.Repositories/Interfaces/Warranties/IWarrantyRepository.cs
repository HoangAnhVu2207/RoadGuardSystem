namespace RoadGuardSystem.Repositories.Warranties;

public interface IWarrantyRepository
{
    Task<WarrantyCreationFacts> GetFactsAsync(
        Guid projectId,
        Guid? roadSectionId,
        Guid? handoverDocumentId,
        Guid? sourceDocumentId,
        CancellationToken cancellationToken = default);

    Task<WarrantyCreationPersistenceResult> CreateAsync(
        WarrantyCreationPersistenceRequest request,
        CancellationToken cancellationToken = default);
}
