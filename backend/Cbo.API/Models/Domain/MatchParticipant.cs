using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Models.Domain;

public class MatchParticipant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int TournamentParticipantId { get; set; }

    [ForeignKey("TournamentParticipantId")]
    public TournamentParticipant TournamentParticipant { get; set; }

    [Required]
    public int MatchId { get; set; }

    [ForeignKey("MatchId")]
    public Match Match { get; set; }

    [Required]
    public int? PromotedFromId { get; set; }

    [ForeignKey("SourceId")]
    public MatchParticipant? PromotedFrom { get; set; }

    public MatchParticipant? PromotedTo { get; set; }

    public ICollection<RoundAnswer> RoundAnswers { get; set; }
}
