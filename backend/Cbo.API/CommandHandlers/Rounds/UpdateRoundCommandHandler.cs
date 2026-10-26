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
/// Replaces a round's answers, then recalculates the match's and the tournament's scores. The round's topic, number
/// and scoring mode can't change.
/// </summary>
public sealed class UpdateRoundCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    IMatchScoringService matchScoringService) : CommandHandler<UpdateRoundCommand>(dbContext)
{
    private const string ManageRoundsForbiddenMessage = "You don't have permission to manage rounds in this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IMatchScoringService _matchScoringService = matchScoringService;

    protected override async Task<Result> ExecuteAsync(UpdateRoundCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ManageRounds, ManageRoundsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Match? match = await _matchScoringService.LoadMatchAsync(command.TournamentId, command.MatchId, cancellationToken);
        if (match is null)
            return RoundFailures.MatchNotFound();

        Round? round = match.Rounds.FirstOrDefault(r => r.NumberInMatch == command.RoundNumber);
        if (round is null)
            return RoundFailures.RoundNotFound(command.RoundNumber);

        if (command.NumberInMatch != command.RoundNumber)
            return RoundFailures.InvalidField("numberInMatch", "Round number in URL must match round number in body.");

        Result<Dictionary<Guid, Question>> validation = await RoundRules.ValidateAsync(
            DbContext, match, command.NumberInMatch, command.TopicId, command.IsOverrideMode, command.Answers, cancellationToken);
        if (validation.IsFailure)
            return validation.Failure;

        if (round.TopicId != command.TopicId || round.IsOverrideMode != command.IsOverrideMode)
            return Failure.Conflict("The topic, number and scoring mode of an existing round can't be changed. Delete the round and create a new one.");

        DbContext.RoundAnswers.RemoveRange(round.RoundAnswers.ToList());
        round.RoundAnswers.Clear();

        List<RoundAnswer> answers = RoundRules.ToRoundAnswers(command.Answers, round.Id, validation.Value);
        foreach (RoundAnswer answer in answers)
            round.RoundAnswers.Add(answer);
        DbContext.RoundAnswers.AddRange(answers);

        await _matchScoringService.RecalculateScoresAsync(match, cancellationToken);

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(UpdateRoundCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous update of the same round: its answers were already replaced
        if (PostgresConstraint.IsUniqueViolation(exception, RoundAnswerConfiguration.RoundIdQuestionIdMatchParticipantIdIndexName))
            return Failure.Conflict($"Round {command.RoundNumber} was changed by someone else at the same time. Reload it and try again.");

        return null;
    }
}
