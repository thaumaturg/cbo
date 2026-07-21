using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public void Configure(EntityTypeBuilder<TournamentParticipant> entity)
    {
        entity.HasKey(tp => tp.Id);

        entity.Property(tp => tp.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(tp => tp.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(TournamentParticipantRole.Player);

        entity.Property(tp => tp.TournamentId)
            .IsRequired();

        entity.Property(tp => tp.ApplicationUserId)
            .IsRequired();

        // Many-to-one: TournamentParticipant -> Tournament
        entity.HasOne(tp => tp.Tournament)
            .WithMany(t => t.TournamentParticipants)
            .HasForeignKey(tp => tp.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: TournamentParticipant -> ApplicationUser
        entity.HasOne(tp => tp.ApplicationUser)
            .WithMany(au => au.TournamentParticipants)
            .HasForeignKey(tp => tp.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: TournamentParticipant -> MatchParticipants
        entity.HasMany(tp => tp.MatchParticipants)
            .WithOne(mp => mp.TournamentParticipant)
            .HasForeignKey(mp => mp.TournamentParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: TournamentParticipant -> TournamentTopics
        entity.HasMany(tp => tp.TournamentTopics)
            .WithOne(tt => tt.TournamentParticipant)
            .HasForeignKey(tt => tt.TournamentParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
