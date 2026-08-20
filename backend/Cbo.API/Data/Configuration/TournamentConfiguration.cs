using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(e => e.Title)
            .IsRequired();

        entity.Property(e => e.CurrentStage)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(TournamentStage.Preparations);


        entity.Property(e => e.StartedAt)
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(e => e.EndedAt)
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(e => e.PlayersPerTournament)
            .HasDefaultValue(DefaultSettings.PlayersPerTournament)
            .IsRequired();

        entity.Property(e => e.TopicsPerParticipantMax)
            .HasDefaultValue(DefaultSettings.TopicsPerParticipantMax)
            .IsRequired();

        entity.Property(e => e.TopicsPerParticipantMin)
            .HasDefaultValue(DefaultSettings.TopicsPerParticipantMin)
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
    }
}
