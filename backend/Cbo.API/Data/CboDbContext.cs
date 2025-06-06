using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Data;

public class CboDbContext : DbContext
{
    public CboDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
        
    }

    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchParticipant> MatchParticipants { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Round> Rounds { get; set; }
    public DbSet<RoundAnswer> RoundAnswers { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<TopicAuthor> TopicAuthors { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
    public DbSet<TournamentTopic> TournamentTopics { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Match
        modelBuilder.Entity<Match>()
            .Property(e => e.Type)
            .HasConversion<string>();

        // MatchParticipant
        modelBuilder.Entity<MatchParticipant>()
            .HasOne(mp => mp.SourceMatch)
            .WithMany(m => m.SourceForMatchParticipants)
            .HasForeignKey(mp => mp.SourceMatchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Question

        // Round

        // RoundAnswer

        // Settings
        modelBuilder.Entity<Settings>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ParticipantsPerMatch)
                .HasDefaultValue(DefaultSettings.TournamentSettings["ParticipantsPerMatch"])
                .IsRequired();

            entity.Property(e => e.ParticipantsPerTournament)
                .HasDefaultValue(DefaultSettings.TournamentSettings["ParticipantsPerTournament"])
                .IsRequired();

            entity.Property(e => e.QuestionsCostMax)
                .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsCostMax"])
                .IsRequired();

            entity.Property(e => e.QuestionsCostMin)
                .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsCostMin"])
                .IsRequired();

            entity.Property(e => e.QuestionsPerTopicMax)
                .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsPerTopicMax"])
                .IsRequired();

            entity.Property(e => e.QuestionsPerTopicMin)
                .HasDefaultValue(DefaultSettings.TournamentSettings["QuestionsPerTopicMin"])
                .IsRequired();

            entity.Property(e => e.TopicsAuthorsMax)
                .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsAuthorsMax"])
                .IsRequired();

            entity.Property(e => e.TopicsPerParticipantMax)
                .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsPerParticipantMax"])
                .IsRequired();

            entity.Property(e => e.TopicsPerParticipantMin)
                .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsPerParticipantMin"])
                .IsRequired();

            entity.Property(e => e.TopicsPerMatch)
                .HasDefaultValue(DefaultSettings.TournamentSettings["TopicsPerMatch"])
                .IsRequired();

            // One-to-one: Settings <->Tournament
             entity.HasOne(s => s.Tournament)
                 .WithOne(t => t.Settings)
                 .HasForeignKey<Settings>(s => s.TournamentId)
                 .OnDelete(DeleteBehavior.Cascade)
                 .IsRequired();
        });

        // Topic

        // TopicAuthor

        // Tournament
        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired();

            entity.Property(e => e.CurrentStage)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(TournamentStage.Preparations);

            entity.Property(e => e.PlannedStart)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StartedAt)
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.EndedAt)
                .ValueGeneratedOnAddOrUpdate();

            // One-to-one: Tournament <-> Settings
            entity.HasOne(t => t.Settings)
                .WithOne(s => s.Tournament)
                .HasForeignKey<Settings>(s => s.TournamentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // One-to-many: Tournament -> TournamentParticipants
            // entity.HasMany(t => t.TournamentParticipants)
            //     .WithOne(tp => tp.Tournament)
            //     .HasForeignKey(tp => tp.TournamentId)
            //     .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Tournament -> TournamentTopics
            // entity.HasMany(t => t.TournamentTopics)
            //     .WithOne(tt => tt.Tournament)
            //     .HasForeignKey(tt => tt.TournamentId)
            //     .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Tournament -> Matches
            // entity.HasMany(t => t.Matches)
            //     .WithOne(m => m.Tournament)
            //     .HasForeignKey(m => m.TournamentId)
            //     .OnDelete(DeleteBehavior.Cascade);
        });

        // TournamentParticipant
        modelBuilder.Entity<TournamentParticipant>()
            .Property(e => e.Role)
            .HasConversion<string>();

        // TournamentTopic

        // User

        // UserPermission
        modelBuilder.Entity<UserPermission>()
            .Property(e => e.PermissionName)
            .HasConversion<string>();
    }
}
