using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Domain.Models;

public class RoundAnswer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public bool IsAnswerAccepted { get; set; }

    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime AnsweredAt { get; set; }

    [Required]
    public int RoundId { get; set; }

    [ForeignKey("RoundId")]
    public Round Round { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public Question Question { get; set; }

    [Required]
    public int MatchParticipantId { get; set; }

    [ForeignKey("MatchParticipantId")]
    public MatchParticipant MatchParticipant { get; set; }
} 
