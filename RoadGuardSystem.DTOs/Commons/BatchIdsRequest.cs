using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Commons;

public sealed class BatchIdsRequest
{
    [Required(ErrorMessage = "Danh sach ID khong duoc rong")]
    [MinLength(1, ErrorMessage = "Phai co toi thieu 1 ID")]
    public List<Guid> Ids { get; set; } = [];
}
