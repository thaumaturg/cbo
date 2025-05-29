using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Models.Domain;

public class Round
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int RoundInSeries { get; set; }

    [Required]
    public int TopicId { get; set; }

    [ForeignKey("TopicId")]
    public Topic Topic { get; set; }

    [Required]
    public int MatchId { get; set; }

    [ForeignKey("MatchId")]
    public Match Match { get; set; }

    public ICollection<RoundAnswer> RoundAnswers { get; set; }
} 
