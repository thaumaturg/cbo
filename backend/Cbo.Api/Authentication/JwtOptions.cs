using System.ComponentModel.DataAnnotations;

namespace Cbo.Api.Authentication;

/// <summary>
/// JWT settings bound from the "Jwt" configuration section and validated at startup.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int LifetimeDaysMin = 1;
    public const int LifetimeDaysMax = 365;
    public const int LifetimeDaysDefault = 14;

    /// <summary>The secret that signs and validates tokens.</summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>The issuer written into and expected on every token.</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The audience written into and expected on every token.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>How many days an issued token stays valid.</summary>
    [Range(LifetimeDaysMin, LifetimeDaysMax)]
    public int LifetimeDays { get; set; } = LifetimeDaysDefault;
}
