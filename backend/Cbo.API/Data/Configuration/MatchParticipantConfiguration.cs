using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> entity)
    {
        entity.HasKey(mp => mp.Id);

        entity.Property(mp => mp.Id)
            .HasDefaultValueSql("uuidv7()");

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
    }
}
