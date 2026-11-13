using Cbo.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.Core.Data.Configuration;

public class TournamentTopicConfiguration : IEntityTypeConfiguration<TournamentTopic>
{
    public const string TournamentIdTopicIdIndexName = "ix_tournament_topics_tournament_id_topic_id";
    public const string TournamentParticipantIdPriorityIndexIndexName = "ix_tournament_topics_tournament_participant_id_priority_index";

    public void Configure(EntityTypeBuilder<TournamentTopic> entity)
    {
        entity.HasKey(tt => tt.Id);

        entity.Property(tt => tt.PriorityIndex)
            .IsRequired();

        entity.Property(tt => tt.IsApproved)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(tt => tt.TournamentId)
            .IsRequired();

        entity.Property(tt => tt.TopicId)
            .IsRequired();

        entity.Property(tt => tt.TournamentParticipantId)
            .IsRequired();

        // Many-to-one: TournamentTopic -> Tournament
        entity.HasOne(tt => tt.Tournament)
            .WithMany(t => t.TournamentTopics)
            .HasForeignKey(tt => tt.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: TournamentTopic -> Topic
        entity.HasOne(tt => tt.Topic)
            .WithMany(t => t.TournamentTopics)
            .HasForeignKey(tt => tt.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: TournamentTopic -> TournamentParticipant
        entity.HasOne(tt => tt.TournamentParticipant)
            .WithMany(tp => tp.TournamentTopics)
            .HasForeignKey(tt => tt.TournamentParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        // A topic is in a tournament at most once.
        // Only a topic's owner can bring it, and an account is a participant at most once
        // Match details rely on it to find a round's tournament topic by topic id.
        entity.HasIndex(tt => new { tt.TournamentId, tt.TopicId })
            .IsUnique()
            .HasDatabaseName(TournamentIdTopicIdIndexName);

        // A participant takes each priority position at most once (ADR-0007: the maximum counts positions)
        entity.HasIndex(tt => new { tt.TournamentParticipantId, tt.PriorityIndex })
            .IsUnique()
            .HasDatabaseName(TournamentParticipantIdPriorityIndexIndexName);
    }
}
