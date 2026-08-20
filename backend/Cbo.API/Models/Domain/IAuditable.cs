namespace Cbo.API.Models.Domain;

/// <summary>
/// Audit timestamps maintained by <c>AuditSaveChangesInterceptor</c>:
/// <see cref="CreatedAt"/> is set on insert, <see cref="UpdatedAt"/> on every update.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
