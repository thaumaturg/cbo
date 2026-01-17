using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Models.Domain;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }

    // Navigation properties
    public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = [];
    public ICollection<TopicAuthor> TopicAuthors { get; set; } = [];
}
