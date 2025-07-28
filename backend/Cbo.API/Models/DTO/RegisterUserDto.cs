using System.ComponentModel.DataAnnotations;

namespace Cbo.API.Models.DTO;

public class RegisterUserDto
{
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
    public required string Username { get; set; }
    public string? FullName { get; set; }
    public string[]? Roles { get; set; }
}
