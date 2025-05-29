using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Models.Domain;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    [Required]
    public bool EmailValidated { get; set; }

    public ICollection<UserPermission> UserPermissions { get; set; }

    public ICollection<TournamentParticipant> TournamentParticipants { get; set; }

    public ICollection<TopicAuthor> TopicAuthors { get; set; }
} 
