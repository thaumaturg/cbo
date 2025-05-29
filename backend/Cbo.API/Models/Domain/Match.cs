using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cbo.API.Domain.Constants;

namespace Cbo.API.Domain.Models;

public class Match
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int MatchInSeries { get; set; }

    [Required]
    public TournamentStage CreatedOnStage { get; set; }

    [Required]
    public Constants.MatchType Type { get; set; }

    [Required]
    public bool IsFinished { get; set; }

    [Required]
    public int TournamentId { get; set; }

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    public ICollection<Round> Rounds { get; set; }

    public ICollection<MatchParticipant> MatchParticipants { get; set; }

    public ICollection<MatchParticipant> SourceForMatchParticipants { get; set; }
} 
