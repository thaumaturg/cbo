using Cbo.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.Core.Data.Configuration;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public const string TournamentIdNumberInTournamentIndexName = "ix_matches_tournament_id_number_in_tournament";
    public const string SettledMatchIdShootoutNumberIndexName = "ix_matches_settled_match_id_shootout_number";

    public void Configure(EntityTypeBuilder<Match> entity)
    {
        entity.HasKey(m => m.Id);

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

        entity.Property(m => m.BracketPosition)
            .HasConversion<string>();

        // A tournament has each match number at most once; also stops two simultaneous Starts from generating the
        // matches twice. A playoff match's number is fixed by its bracket position, so it also stops the same playoff
        // match from being created twice
        entity.HasIndex(m => new { m.TournamentId, m.NumberInTournament })
            .IsUnique()
            .HasDatabaseName(TournamentIdNumberInTournamentIndexName);

        // A match has each shootout number at most once
        entity.HasIndex(m => new { m.SettledMatchId, m.ShootoutNumber })
            .IsUnique()
            .HasDatabaseName(SettledMatchIdShootoutNumberIndexName);

        // Many-to-one: Match -> Tournament
        entity.HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: shootout -> the match it settles, which takes its shootouts with it when deleted
        entity.HasOne(m => m.SettledMatch)
            .WithMany(m => m.Shootouts)
            .HasForeignKey(m => m.SettledMatchId)
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
