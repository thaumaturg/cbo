using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> entity)
    {
        entity.HasKey(m => m.Id);

        entity.Property(m => m.Id)
            .HasDefaultValueSql("uuidv7()");

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
    }
}
