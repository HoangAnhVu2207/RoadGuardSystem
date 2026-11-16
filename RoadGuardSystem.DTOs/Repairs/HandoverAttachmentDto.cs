using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record HandoverAttachmentDto(Guid FileId, Guid? HandoverDocumentId = null,
    string? DocumentNo = null, DateOnly? HandoverDate = null);
