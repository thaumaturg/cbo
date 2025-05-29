using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Models.Domain;

public class Settings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ParticipantsPerMatchMax { get; set; }

    [Required]
    public int ParticipantsPerTournamentMax { get; set; }

    [Required]
    public int ParticipantsPerTournamentMin { get; set; }

    [Required]
    public int QuestionsCostMax { get; set; }

    [Required]
    public int QuestionsCostMin { get; set; }

    [Required]
    public int QuestionsPerTopicMax { get; set; }

    [Required]
    public int QuestionsPerTopicMin { get; set; }

    [Required]
    public int TopicsAuthorsMax { get; set; }

    [Required]
    public int TopicsPerParticipantMax { get; set; }

    [Required]
    public int TopicsPerParticipantMin { get; set; }

    [Required]
    public int TopicsPerMatch { get; set; }

    [Required]
    public int TournamentId { get; set; }

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }
} 
