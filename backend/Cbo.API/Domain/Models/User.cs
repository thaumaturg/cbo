namespace Cbo.API.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool EmailValidated { get; set; }

    public ICollection<UserPermission> UserPermissions { get; set; }
    public ICollection<TournamentParticipant> TournamentParticipants { get; set; }
    public ICollection<TopicAuthor> TopicAuthors { get; set; }
} 
