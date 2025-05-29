using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class Tournament
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public TournamentStage CurrentStage { get; set; }

    [Required]
    public DateTime PlannedStart { get; set; }

    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime? StartedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime? EndedAt { get; set; }

    public ICollection<TournamentParticipant> TournamentParticipants { get; set; }

    public ICollection<TournamentTopic> TournamentTopics { get; set; }

    public ICollection<Match> Matches { get; set; }

    public Settings Settings { get; set; }
} 
