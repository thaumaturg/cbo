using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public const string MatchIdNumberInMatchIndexName = "ix_rounds_match_id_number_in_match";

    public void Configure(EntityTypeBuilder<Round> entity)
    {
        entity.HasKey(r => r.Id);

        entity.Property(r => r.NumberInMatch)
            .IsRequired();

        entity.Property(r => r.TopicId)
            .IsRequired();

        entity.Property(r => r.MatchId)
            .IsRequired();

        // A match has each round number at most once; also stops two simultaneous creates of the same round
        entity.HasIndex(r => new { r.MatchId, r.NumberInMatch })
            .IsUnique()
            .HasDatabaseName(MatchIdNumberInMatchIndexName);

        // Many-to-one: Round -> Match
        entity.HasOne(r => r.Match)
            .WithMany(m => m.Rounds)
            .HasForeignKey(r => r.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: Round -> Topic
        // Restrict: a played topic must not cascade-delete its rounds (played match data)
        entity.HasOne(r => r.Topic)
            .WithMany(t => t.Rounds)
            .HasForeignKey(r => r.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: Round -> RoundAnswers
        entity.HasMany(r => r.RoundAnswers)
            .WithOne(ra => ra.Round)
            .HasForeignKey(ra => ra.RoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
