using System.ComponentModel.DataAnnotations;

namespace Cbo.Api.Contracts.Auth;

public record ChangePasswordRequest
{
    [DataType(DataType.Password)]
    public required string CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    public required string NewPassword { get; set; }
}
