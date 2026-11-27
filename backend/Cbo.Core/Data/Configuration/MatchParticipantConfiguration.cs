using Cbo.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.Core.Data.Configuration;

public class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> entity)
    {
        entity.HasKey(mp => mp.Id);

        entity.Property(mp => mp.TournamentParticipantId)
            .IsRequired();

        entity.Property(mp => mp.MatchId)
            .IsRequired();

        entity.Property(mp => mp.Outcome)
            .HasConversion<string>();

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

        // One-to-many: MatchParticipant -> RoundAnswers
        // Restrict: given answers (played match data) must not silently cascade away
        entity.HasMany(mp => mp.RoundAnswers)
            .WithOne(ra => ra.MatchParticipant)
            .HasForeignKey(ra => ra.MatchParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
