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
        // Restrict: removing a participant must not cascade-delete their match history
        entity.HasOne(mp => mp.TournamentParticipant)
            .WithMany(tp => tp.MatchParticipants)
            .HasForeignKey(mp => mp.TournamentParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

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
        // Restrict: given answers (played match data) must not silently cascade away
        entity.HasMany(mp => mp.RoundAnswers)
            .WithOne(ra => ra.MatchParticipant)
            .HasForeignKey(ra => ra.MatchParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
