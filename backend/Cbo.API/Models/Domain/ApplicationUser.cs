using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Models.Domain;

public class ApplicationUser : IdentityUser
{
    public string? Name { get; set; }
    public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = [];
    public ICollection<TopicAuthor> TopicAuthors { get; set; } = [];
}
