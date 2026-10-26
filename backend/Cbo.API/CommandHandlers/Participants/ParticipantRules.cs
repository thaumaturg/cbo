using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Participants;

/// <summary>
/// Organizer limit and player seeding rules shared by the participant command handlers. Reads and stages only.
/// </summary>
internal static class ParticipantRules
{
    public static async Task<bool> HasMaxOrganizersAsync(CboDbContext dbContext, Guid tournamentId, CancellationToken cancellationToken)
    {
        int organizerCount = await dbContext.TournamentParticipants
            .CountAsync(tp => tp.TournamentId == tournamentId && tp.Role == TournamentParticipantRole.Organizer, cancellationToken);

        return organizerCount >= DefaultSettings.OrganizersPerTournamentMax;
    }

    /// <summary>
    /// Returns the seed for a newly added player: one past the highest seed currently taken.
    /// </summary>
    public static async Task<int> GetNextPlayerSeedAsync(CboDbContext dbContext, Guid tournamentId, CancellationToken cancellationToken)
    {
        int? highestSeed = await dbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId && tp.Role == TournamentParticipantRole.Player)
            .MaxAsync(tp => (int?)(tp.Seed ?? 0), cancellationToken);

        return (highestSeed ?? 0) + 1;
    }

    /// <summary>
    /// Renumbers the seeds of the players who stay to a contiguous 1..N sequence, preserving their order.
    /// The leaving player is excluded explicitly: their removal or demotion is only staged, so the database still lists them.
    /// </summary>
    public static async Task CompactPlayerSeedsAsync(
        CboDbContext dbContext,
        Guid tournamentId,
        Guid leavingParticipantId,
        CancellationToken cancellationToken)
    {
        List<TournamentParticipant> remainingPlayers = await dbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId
                && tp.Role == TournamentParticipantRole.Player
                && tp.Id != leavingParticipantId)
            .ToListAsync(cancellationToken);

        List<TournamentParticipant> orderedPlayers = remainingPlayers
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.Id)
            .ToList();

        for (int position = 0; position < orderedPlayers.Count; position++)
            orderedPlayers[position].Seed = position + 1;
    }
}
