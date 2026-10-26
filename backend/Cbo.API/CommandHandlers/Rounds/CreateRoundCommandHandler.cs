using Cbo.API.Authorization;
using Cbo.API.Commands.Rounds;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Rounds;

/// <summary>
/// Adds a round with its answers to the match, then recalculates the match's and the tournament's scores.
/// </summary>
public sealed class CreateRoundCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    IMatchScoringService matchScoringService) : CommandHandler<CreateRoundCommand>(dbContext)
{
    private const string ManageRoundsForbiddenMessage = "You don't have permission to manage rounds in this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IMatchScoringService _matchScoringService = matchScoringService;

    protected override async Task<Result> ExecuteAsync(CreateRoundCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ManageRounds, ManageRoundsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Match? match = await _matchScoringService.LoadMatchAsync(command.TournamentId, command.MatchId, cancellationToken);
        if (match is null)
            return RoundFailures.MatchNotFound();

        Result<Dictionary<Guid, Question>> validation = await RoundRules.ValidateAsync(
            DbContext, match, command.NumberInMatch, command.TopicId, command.IsOverrideMode, command.Answers, cancellationToken);
        if (validation.IsFailure)
            return validation.Failure;

        if (match.Rounds.Any(r => r.NumberInMatch == command.NumberInMatch))
            return RoundFailures.RoundAlreadyExists(command.NumberInMatch);

        // Set here rather than by the database default, so the answers can reference the round
        var round = new Round
        {
            Id = Guid.CreateVersion7(),
            NumberInMatch = command.NumberInMatch,
            IsOverrideMode = command.IsOverrideMode,
            TopicId = command.TopicId,
            MatchId = match.Id,
        };
        round.RoundAnswers = RoundRules.ToRoundAnswers(command.Answers, round.Id, validation.Value);

        match.Rounds.Add(round);
        DbContext.Rounds.Add(round);

        await _matchScoringService.RecalculateScoresAsync(match, cancellationToken);

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(CreateRoundCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous create of the same round
        if (PostgresConstraint.IsUniqueViolation(exception, RoundConfiguration.MatchIdNumberInMatchIndexName))
            return RoundFailures.RoundAlreadyExists(command.NumberInMatch);

        return null;
    }
}
