using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpPost]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds")]
    [Authorize]
    public async Task<IActionResult> CreateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromBody] CreateRoundWithAnswersDto createRoundDto)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageRounds);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        if (createRoundDto.NumberInMatch < 1 || createRoundDto.NumberInMatch > DefaultSettings.RoundsPerMatch)
            return Problem(detail: $"Round number must be between 1 and {DefaultSettings.RoundsPerMatch}.", statusCode: StatusCodes.Status400BadRequest);

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, createRoundDto.NumberInMatch);
        if (existingRound is not null)
            return Problem(detail: $"Round {createRoundDto.NumberInMatch} already exists for this match. Use PUT to update.", statusCode: StatusCodes.Status400BadRequest);

        Topic? topic = await _topicRepository.GetByIdAsync(createRoundDto.TopicId);
        if (topic is null)
            return Problem(detail: "Topic not found.", statusCode: StatusCodes.Status400BadRequest);

        string? answersValidationError = _roundService.ValidateRoundAnswers(createRoundDto.Answers, createRoundDto.IsOverrideMode);
        if (answersValidationError is not null)
            return Problem(detail: answersValidationError, statusCode: StatusCodes.Status400BadRequest);

        Round round = createRoundDto.ToNewRound(matchId);

        foreach (CreateRoundAnswerDto answerDto in createRoundDto.Answers)
        {
            RoundAnswer answer = answerDto.ToNewRoundAnswer(Guid.Empty);
            round.RoundAnswers.Add(answer);
        }

        await using (IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                await _roundRepository.CreateAsync(round);
                await _roundService.RecalculateMatchScoresAsync(matchId);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        Round? createdRound = await _roundRepository.GetByIdWithDetailsAsync(round.Id);
        if (createdRound is null)
            return Problem("Failed to retrieve created round.");

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        TournamentTopic? tournamentTopic = tournamentTopics.FirstOrDefault(tt => tt.TopicId == createdRound.TopicId);
        if (tournamentTopic is null)
            return Problem("Failed to find tournament topic for round.");

        int priorityIndex = tournamentTopic.PriorityIndex;
        string ownerUsername = tournamentTopic.TournamentParticipant.ApplicationUser?.UserName ?? string.Empty;

        GetRoundDto roundDto = createdRound.ToGetDto(priorityIndex, ownerUsername);
        return CreatedAtAction(nameof(CreateRound), new { tournamentId, matchId, roundNumber = roundDto.NumberInMatch }, roundDto);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        [FromBody] CreateRoundWithAnswersDto updateRoundDto)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageRounds);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        if (updateRoundDto.NumberInMatch != roundNumber)
            return Problem(detail: "Round number in URL must match round number in body.", statusCode: StatusCodes.Status400BadRequest);

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is null)
            return Problem(detail: $"Round {roundNumber} not found for this match.", statusCode: StatusCodes.Status404NotFound);

        if (existingRound.TopicId != updateRoundDto.TopicId ||
            existingRound.NumberInMatch != updateRoundDto.NumberInMatch ||
            existingRound.IsOverrideMode != updateRoundDto.IsOverrideMode)
            return Problem(detail: "Cannot change existing round. Delete and create a new one.", statusCode: StatusCodes.Status400BadRequest);

        string? answersValidationError = _roundService.ValidateRoundAnswers(updateRoundDto.Answers, updateRoundDto.IsOverrideMode);
        if (answersValidationError is not null)
            return Problem(detail: answersValidationError, statusCode: StatusCodes.Status400BadRequest);

        List<RoundAnswer> newAnswers = updateRoundDto.Answers
            .Select(a => a.ToNewRoundAnswer(existingRound.Id))
            .ToList();

        await using (IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                await _roundRepository.DeleteAnswersByRoundIdAsync(existingRound.Id);
                await _roundRepository.CreateAnswersAsync(newAnswers);
                await _roundService.RecalculateMatchScoresAsync(matchId);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        Round? updatedRound = await _roundRepository.GetByIdWithDetailsAsync(existingRound.Id);
        if (updatedRound is null)
            return Problem("Failed to retrieve updated round.");

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        TournamentTopic? tournamentTopic = tournamentTopics.FirstOrDefault(tt => tt.TopicId == updatedRound.TopicId);
        if (tournamentTopic is null)
            return Problem("Failed to find tournament topic for round.");

        int priorityIndex = tournamentTopic.PriorityIndex;
        string ownerUsername = tournamentTopic.TournamentParticipant.ApplicationUser?.UserName ?? string.Empty;

        GetRoundDto roundDto = updatedRound.ToGetDto(priorityIndex, ownerUsername);
        return Ok(roundDto);
    }

    [HttpDelete]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageRounds);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is null)
            return Problem(detail: $"Round {roundNumber} not found for this match.", statusCode: StatusCodes.Status404NotFound);

        await using (IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                await _roundRepository.DeleteAsync(existingRound.Id);
                await _roundService.RecalculateMatchScoresAsync(matchId);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return NoContent();
    }
}
