using System.ComponentModel.DataAnnotations;

namespace Cbo.API.Contracts.Auth;

public record LoginRequest
{
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
}
