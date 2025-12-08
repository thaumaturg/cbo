using System.ComponentModel.DataAnnotations;

namespace Cbo.API.Models.DTO;

public record RegisterUserDto
{
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
    public required string Username { get; set; }
    public string? FullName { get; set; }
}
