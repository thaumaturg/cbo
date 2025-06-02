using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Data;

public class CboDbContext : DbContext
{
    public CboDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
        
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Round> Rounds { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
    public DbSet<TournamentTopic> TournamentTopics { get; set; }
    public DbSet<TopicAuthor> TopicAuthors { get; set; }
    public DbSet<MatchParticipant> MatchParticipants { get; set; }
    public DbSet<RoundAnswer> RoundAnswers { get; set; }
    public DbSet<Settings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserPermission>()
            .Property(e => e.PermissionName)
            .HasConversion<string>();
        modelBuilder.Entity<TournamentParticipant>()
            .Property(e => e.Role)
            .HasConversion<string>();
        modelBuilder.Entity<Match>()
            .Property(e => e.Type)
            .HasConversion<string>();
        modelBuilder.Entity<Tournament>()
            .Property(e => e.CurrentStage)
            .HasConversion<string>()
            .HasDefaultValue(TournamentStage.Preparations);
        modelBuilder.Entity<Tournament>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<MatchParticipant>()
            .HasOne(mp => mp.SourceMatch)
            .WithMany(m => m.SourceForMatchParticipants)
            .HasForeignKey(mp => mp.SourceMatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Settings>()
            .HasOne(s => s.Tournament)
            .WithOne(t => t.Settings)
            .HasForeignKey<Settings>(s => s.TournamentId)
            .IsRequired();
        modelBuilder.Entity<Settings>()
            .Property(e => e.ParticipantsPerMatch)
            .HasDefaultValue(DefaultSettings.TournamentSettings["ParticipantsPerMatch"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.ParticipantsPerTournament)
            .HasDefaultValue(DefaultSettings.TournamentSettings["ParticipantsPerTournament"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.QuestionsCostMax)
            .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsCostMax"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.QuestionsCostMin)
            .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsCostMin"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.QuestionsPerTopicMax)
            .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsPerTopicMax"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.QuestionsPerTopicMin)
            .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsPerTopicMin"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.TopicsAuthorsMax)
            .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsAuthorsMax"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.TopicsPerParticipantMax)
            .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsPerParticipantMax"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.TopicsPerParticipantMin)
            .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsPerParticipantMin"]);
        modelBuilder.Entity<Settings>()
            .Property(e => e.TopicsPerMatch)
            .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsPerMatch"]);
    }
}
