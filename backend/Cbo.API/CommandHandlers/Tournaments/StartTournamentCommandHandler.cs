using Cbo.API.Authorization;
using Cbo.API.Commands.Tournaments;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Tournaments;

/// <summary>
/// Moves a tournament from Preparations to Qualifications and generates its qualification matches.
/// </summary>
public sealed class StartTournamentCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    IMatchGenerationService matchGenerationService) : CommandHandler<StartTournamentCommand>(dbContext)
{
    private const string StartForbiddenMessage = "You don't have permission to start this tournament.";
    private const string AlreadyStartedMessage = "The tournament has already started. It can only advance to Qualifications from the Preparations stage.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IMatchGenerationService _matchGenerationService = matchGenerationService;

    protected override async Task<Result> ExecuteAsync(StartTournamentCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.AdvanceStage, StartForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict(AlreadyStartedMessage);

        List<TournamentParticipant> participants = await DbContext.TournamentParticipants
            .AsNoTracking()
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.TournamentTopics)
            .Where(tp => tp.TournamentId == command.TournamentId)
            .ToListAsync(cancellationToken);

        List<TournamentParticipant> players = participants
            .Where(p => p.Role == TournamentParticipantRole.Player)
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.Id)
            .ToList();

        if (players.Count != tournament.PlayersPerTournament)
            return Failure.Conflict($"The tournament needs exactly {tournament.PlayersPerTournament} players to start. It has {players.Count}.");

        Result topicCountCheck = CheckTopicCounts(tournament, participants);
        if (topicCountCheck.IsFailure)
            return topicCountCheck.Failure;

        List<Match> matches = _matchGenerationService.GenerateQualificationMatches(tournament, players);
        DbContext.Matches.AddRange(matches);

        tournament.CurrentStage = TournamentStage.Qualifications;
        tournament.StartedAt = DateTime.UtcNow;

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(StartTournamentCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous start of the same tournament
        if (PostgresConstraint.IsUniqueViolation(exception, MatchConfiguration.TournamentIdNumberInTournamentIndexName))
            return Failure.Conflict(AlreadyStartedMessage);

        return null;
    }

    /// <summary>
    /// Every player needs between the minimum and the maximum number of topics; other participants at most the maximum.
    /// </summary>
    private static Result CheckTopicCounts(Tournament tournament, List<TournamentParticipant> participants)
    {
        foreach (TournamentParticipant participant in participants)
        {
            int topicCount = participant.TournamentTopics.Count;
            string name = participant.ApplicationUser?.UserName ?? participant.Id.ToString();

            if (participant.Role == TournamentParticipantRole.Player && topicCount < tournament.TopicsPerParticipantMin)
                return Failure.Conflict($"Player '{name}' has {topicCount} topics, but needs at least {tournament.TopicsPerParticipantMin}.");

            if (topicCount > tournament.TopicsPerParticipantMax)
                return Failure.Conflict($"Participant '{name}' has {topicCount} topics, but the maximum is {tournament.TopicsPerParticipantMax}.");
        }

        return Result.Success();
    }
}
