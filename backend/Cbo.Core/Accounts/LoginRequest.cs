using System.ComponentModel.DataAnnotations;

namespace Cbo.Core.Accounts;

public record LoginRequest
{
    public required string UsernameOrEmail { get; set; }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
}
