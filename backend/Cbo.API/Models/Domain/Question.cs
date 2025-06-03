using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Models.Domain;

public class Question
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int QuestionNumber { get; set; }

    [Required]
    public int CostPositive { get; set; }

    [Required]
    public int CostNegative { get; set; }

    [Required]
    public string Text { get; set; }

    [Required]
    public string Answer { get; set; }

    public string? Comment { get; set; }

    [Required]
    public int TopicId { get; set; }

    [ForeignKey("TopicId")]
    public Topic Topic { get; set; }

    public ICollection<RoundAnswer> RoundAnswers { get; set; }
}
