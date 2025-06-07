using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class Match
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int NumberInTournament { get; set; }

    [Required]
    public int NumberInStage { get; set; }

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
}
