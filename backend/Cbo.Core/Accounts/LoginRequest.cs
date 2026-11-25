using System.ComponentModel.DataAnnotations;
using Cbo.Core.Validation;

namespace Cbo.Core.Accounts;

public record LoginRequest
{
    public required string UsernameOrEmail { get; set => field = RequestText.Trim(value); }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
}
