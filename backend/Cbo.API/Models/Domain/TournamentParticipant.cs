using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cbo.API.Domain.Constants;

namespace Cbo.API.Domain.Models;

public class TournamentParticipant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public TournamentParticipantRole Role { get; set; }

    [Required]
    public int PointsSum { get; set; }

    [Required]
    public int TournamentId { get; set; }

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    public ICollection<MatchParticipant> MatchParticipants { get; set; }
} 
