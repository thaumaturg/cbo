using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Models.Domain;

public class ApplicationUser : IdentityUser<Guid>, IAuditable
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = [];
    public ICollection<TopicAuthor> TopicAuthors { get; set; } = [];
}
