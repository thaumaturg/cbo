using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        if (createRoundDto.NumberInMatch < 1 || createRoundDto.NumberInMatch > 4)
            return BadRequest("Round number must be between 1 and 4.");

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, createRoundDto.NumberInMatch);
        if (existingRound is not null)
            return BadRequest($"Round {createRoundDto.NumberInMatch} already exists for this match. Use PUT to update.");

        Topic? topic = await _topicRepository.GetByIdAsync(createRoundDto.TopicId);
        if (topic is null)
            return BadRequest("Topic not found.");

        string? answersValidationError = _roundService.ValidateRoundAnswers(createRoundDto.Answers, createRoundDto.IsOverrideMode);
        if (answersValidationError is not null)
            return BadRequest(answersValidationError);

        Round round = createRoundDto.ToNewRound(matchId);

        foreach (CreateRoundAnswerDto answerDto in createRoundDto.Answers)
        {
            RoundAnswer answer = answerDto.ToNewRoundAnswer(Guid.Empty);
            round.RoundAnswers.Add(answer);
        }

        await _roundRepository.CreateAsync(round);
        await _roundService.RecalculateMatchScoresAsync(matchId);

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
            return BadRequest("Round number in URL must match round number in body.");

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is null)
            return NotFound($"Round {roundNumber} not found for this match.");

        if (existingRound.TopicId != updateRoundDto.TopicId ||
            existingRound.NumberInMatch != updateRoundDto.NumberInMatch ||
            existingRound.IsOverrideMode != updateRoundDto.IsOverrideMode)
            return BadRequest("Cannot change existing round. Delete and create a new one.");

        string? answersValidationError = _roundService.ValidateRoundAnswers(updateRoundDto.Answers, updateRoundDto.IsOverrideMode);
        if (answersValidationError is not null)
            return BadRequest(answersValidationError);

        await _roundRepository.DeleteAnswersByRoundIdAsync(existingRound.Id);

        List<RoundAnswer> newAnswers = updateRoundDto.Answers
            .Select(a => a.ToNewRoundAnswer(existingRound.Id))
            .ToList();

        await _roundRepository.CreateAnswersAsync(newAnswers);
        await _roundService.RecalculateMatchScoresAsync(matchId);

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
            return NotFound($"Round {roundNumber} not found for this match.");

        await _roundRepository.DeleteAsync(existingRound.Id);
        await _roundService.RecalculateMatchScoresAsync(matchId);

        return NoContent();
    }
}
