using Cbo.API.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Data;

public class CboDbContext(DbContextOptions<CboDbContext> dbContextOptions) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(dbContextOptions)
{
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchParticipant> MatchParticipants { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Round> Rounds { get; set; }
    public DbSet<RoundAnswer> RoundAnswers { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<TopicAuthor> TopicAuthors { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
    public DbSet<TournamentTopic> TournamentTopics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CboDbContext).Assembly);
    }
}
