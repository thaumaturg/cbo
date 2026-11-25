using System.ComponentModel.DataAnnotations;
using Cbo.Core.Validation;

namespace Cbo.Core.Accounts;

public record RegisterRequest
{
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set => field = RequestText.Trim(value); }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
    public required string Username { get; set => field = RequestText.Trim(value); }
    public string? FullName { get; set => field = RequestText.TrimToNull(value); }
}
