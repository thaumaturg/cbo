using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cbo.API.Domain.Models;

public class Topic
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public int PriorityIndex { get; set; }

    [Required]
    public bool IsGuest { get; set; }

    [Required]
    public bool IsPlayed { get; set; }

    public Round? Round { get; set; }

    public ICollection<Question> Questions { get; set; }

    public ICollection<TournamentTopic> TournamentTopics { get; set; }

    public ICollection<TopicAuthor> TopicAuthors { get; set; }
} 
