using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Models.Domain;

public class TopicAuthor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public bool IsOwner { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public int TopicId { get; set; }

    [ForeignKey("TopicId")]
    public Topic Topic { get; set; }
} 
