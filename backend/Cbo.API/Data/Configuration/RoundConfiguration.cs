using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> entity)
    {
        entity.HasKey(r => r.Id);

        entity.Property(r => r.Id)
            .HasDefaultValueSql("uuidv7()");

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
            .WithMany(t => t.Rounds)
            .HasForeignKey(r => r.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Round -> RoundAnswers
        entity.HasMany(r => r.RoundAnswers)
            .WithOne(ra => ra.Round)
            .HasForeignKey(ra => ra.RoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
