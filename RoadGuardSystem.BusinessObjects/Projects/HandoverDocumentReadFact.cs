namespace RoadGuardSystem.BusinessObjects.Projects;

/// <summary>Document inventory, including legacy documents without a file; not coverage eligibility.</summary>
public sealed record HandoverDocumentReadFact(Guid Id, string DocumentNo, DateOnly HandoverDate,
    Guid? FileId, string Version, bool IsMapped, bool CanAttachFile);

public sealed record HandoverDocumentsReadFact(Guid ProjectId, HandoverDocumentReadFact[] Items);
