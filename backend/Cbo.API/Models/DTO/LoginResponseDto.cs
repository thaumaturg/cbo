namespace Cbo.API.Models.DTO;

public record LoginResponseDto
{
    public required string JwtToken { get; set; }
}
