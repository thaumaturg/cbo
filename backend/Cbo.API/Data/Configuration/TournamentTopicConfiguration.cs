using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TournamentTopicConfiguration : IEntityTypeConfiguration<TournamentTopic>
{
    public void Configure(EntityTypeBuilder<TournamentTopic> entity)
    {
        entity.HasKey(tt => tt.Id);

        entity.Property(tt => tt.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(tt => tt.PriorityIndex)
            .IsRequired();

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

        // Unique constraint: One topic can only be added once per tournament per participant
        entity.HasIndex(tt => new { tt.TournamentId, tt.TopicId, tt.TournamentParticipantId })
            .IsUnique();
    }
}
