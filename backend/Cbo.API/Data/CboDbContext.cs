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
        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Id)
                .ValueGeneratedOnAdd();

            entity.Property(m => m.NumberInTournament)
                .IsRequired();

            entity.Property(m => m.NumberInStage)
                .IsRequired();

            entity.Property(m => m.CreatedOnStage)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(m => m.Type)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(m => m.IsFinished)
                .IsRequired();

            entity.Property(m => m.TournamentId)
                .IsRequired();

            // Many-to-one: Match -> Tournament
            entity.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Match -> Rounds
            entity.HasMany(m => m.Rounds)
                .WithOne(r => r.Match)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Match -> MatchParticipants
            entity.HasMany(m => m.MatchParticipants)
                .WithOne(mp => mp.Match)
                .HasForeignKey(mp => mp.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MatchParticipant
        modelBuilder.Entity<MatchParticipant>(entity =>
        {
            entity.HasKey(mp => mp.Id);

            entity.Property(mp => mp.Id)
                .ValueGeneratedOnAdd();

            entity.Property(mp => mp.TournamentParticipantId)
                .IsRequired();

            entity.Property(mp => mp.MatchId)
                .IsRequired();

            entity.Property(mp => mp.PromotedFromId)
                .IsRequired(false);

            // Many-to-one: MatchParticipant -> TournamentParticipant
            entity.HasOne(mp => mp.TournamentParticipant)
                .WithMany(tp => tp.MatchParticipants)
                .HasForeignKey(mp => mp.TournamentParticipantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one: MatchParticipant -> Match
            entity.HasOne(mp => mp.Match)
                .WithMany(m => m.MatchParticipants)
                .HasForeignKey(mp => mp.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing: MatchParticipant -> PromotedFrom
            entity.HasOne(mp => mp.PromotedFrom)
                .WithOne(mp2 => mp2.PromotedTo)
                .HasForeignKey<MatchParticipant>(mp => mp.PromotedFromId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-many: MatchParticipant -> RoundAnswers
            entity.HasMany(mp => mp.RoundAnswers)
                .WithOne(ra => ra.MatchParticipant)
                .HasForeignKey(ra => ra.MatchParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Question
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.Id)
                .ValueGeneratedOnAdd();

            entity.Property(q => q.QuestionNumber)
                .IsRequired();

            entity.Property(q => q.CostPositive)
                .IsRequired();

            entity.Property(q => q.CostNegative)
                .IsRequired();

            entity.Property(q => q.Text)
                .IsRequired();

            entity.Property(q => q.Answer)
                .IsRequired();

            entity.Property(q => q.TopicId)
                .IsRequired();

            // Many-to-one: Question -> Topic
            entity.HasOne(q => q.Topic)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Question -> RoundAnswers
            entity.HasMany(q => q.RoundAnswers)
                .WithOne(ra => ra.Question)
                .HasForeignKey(ra => ra.QuestionId);
        });

        // Round
        modelBuilder.Entity<Round>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            entity.Property(r => r.NumberInMatch)
                .IsRequired();

            entity.Property(r => r.TopicId)
                .IsRequired();

            entity.Property(r => r.MatchId)
                .IsRequired();

            // Many-to-one: Round -> Match
            entity.HasOne(r => r.Match)
                .WithMany(m => m.Rounds)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one: Round -> Topic
            entity.HasOne(r => r.Topic)
                .WithOne(t => t.Round)
                .HasForeignKey<Round>(r => r.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Round -> RoundAnswers
            entity.HasMany(r => r.RoundAnswers)
                .WithOne(ra => ra.Round)
                .HasForeignKey(ra => ra.RoundId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoundAnswer
        modelBuilder.Entity<RoundAnswer>(entity =>
        {
            entity.HasKey(ra => ra.Id);

            entity.Property(ra => ra.Id)
                .ValueGeneratedOnAdd();

            entity.Property(ra => ra.IsAnswerAccepted)
                .IsRequired();

            entity.Property(ra => ra.AnsweredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(ra => ra.RoundId)
                .IsRequired();

            entity.Property(ra => ra.QuestionId)
                .IsRequired();

            entity.Property(ra => ra.MatchParticipantId)
                .IsRequired();

            // Many-to-one: RoundAnswer -> Round
            entity.HasOne(ra => ra.Round)
                .WithMany(r => r.RoundAnswers)
                .HasForeignKey(ra => ra.RoundId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one: RoundAnswer -> Question
            entity.HasOne(ra => ra.Question)
                .WithMany(q => q.RoundAnswers)
                .HasForeignKey(ra => ra.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one: RoundAnswer -> MatchParticipant
            entity.HasOne(ra => ra.MatchParticipant)
                .WithMany(mp => mp.RoundAnswers)
                .HasForeignKey(ra => ra.MatchParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

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

            // One-to-one: Settings <-> Tournament
             entity.HasOne(s => s.Tournament)
                 .WithOne(t => t.Settings)
                 .HasForeignKey<Settings>(s => s.TournamentId)
                 .OnDelete(DeleteBehavior.Cascade)
                 .IsRequired();
        });

        // Topic
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            entity.Property(t => t.Title)
                .IsRequired();

            entity.Property(t => t.IsGuest)
                .IsRequired();

            entity.Property(t => t.IsPlayed)
                .IsRequired();

            // One-to-many: Topic -> Questions
            entity.HasMany(t => t.Questions)
                .WithOne(q => q.Topic)
                .HasForeignKey(q => q.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Topic -> TournamentTopics
            entity.HasMany(t => t.TournamentTopics)
                .WithOne(tt => tt.Topic)
                .HasForeignKey(tt => tt.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Topic -> TopicAuthors
            entity.HasMany(t => t.TopicAuthors)
                .WithOne(ta => ta.Topic)
                .HasForeignKey(ta => ta.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: Topic <-> Round
            entity.HasOne(t => t.Round)
                .WithOne(r => r.Topic)
                .HasForeignKey<Round>(r => r.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

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
            entity.HasMany(t => t.TournamentParticipants)
                .WithOne(tp => tp.Tournament)
                .HasForeignKey(tp => tp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Tournament -> TournamentTopics
            entity.HasMany(t => t.TournamentTopics)
                .WithOne(tt => tt.Tournament)
                .HasForeignKey(tt => tt.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Tournament -> Matches
            entity.HasMany(t => t.Matches)
                .WithOne(m => m.Tournament)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
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
