using System.ComponentModel.DataAnnotations;

namespace Cbo.API.Models.DTO;

public record ChangePasswordDto
{
    [DataType(DataType.Password)]
    public required string CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    public required string NewPassword { get; set; }
}
