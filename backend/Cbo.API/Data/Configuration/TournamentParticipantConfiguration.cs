using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public const string TournamentIdApplicationUserIdIndexName = "ix_tournament_participants_tournament_id_application_user_id";

    public void Configure(EntityTypeBuilder<TournamentParticipant> entity)
    {
        entity.HasKey(tp => tp.Id);

        entity.Property(tp => tp.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(TournamentParticipantRole.Player);

        entity.Property(tp => tp.TournamentId)
            .IsRequired();

        entity.Property(tp => tp.ApplicationUserId)
            .IsRequired();

        // A user is a participant of a tournament at most once; also stops two simultaneous adds of the same user
        entity.HasIndex(tp => new { tp.TournamentId, tp.ApplicationUserId })
            .IsUnique()
            .HasDatabaseName(TournamentIdApplicationUserIdIndexName);

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
        // Restrict: removing a participant must not cascade-delete their match history
        entity.HasMany(tp => tp.MatchParticipants)
            .WithOne(mp => mp.TournamentParticipant)
            .HasForeignKey(mp => mp.TournamentParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: TournamentParticipant -> TournamentTopics
        entity.HasMany(tp => tp.TournamentTopics)
            .WithOne(tt => tt.TournamentParticipant)
            .HasForeignKey(tt => tt.TournamentParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
