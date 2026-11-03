using System.ComponentModel.DataAnnotations;

namespace Cbo.API.Models.DTO;

public record LoginRequest
{
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
}
