using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record PointDto(
    [Range(-180, 180)] double Longitude,
    [Range(-90, 90)] double Latitude);
