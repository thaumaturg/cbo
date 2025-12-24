using Cbo.API.Authorization;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:int}/matches")]
    [Authorize]
    public async Task<IActionResult> GetAllMatches([FromRoute] int tournamentId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        List<Match> matchesDomain = await _matchRepository.GetAllByTournamentIdAsync(tournamentId);
        List<GetMatchDto> matchesDto = _mapper.Map<List<GetMatchDto>>(matchesDomain);

        return Ok(matchesDto);
    }

    [HttpGet]
    [Route("{tournamentId:int}/matches/{matchId:int}")]
    [Authorize]
    public async Task<IActionResult> GetMatchById([FromRoute] int tournamentId, [FromRoute] int matchId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdWithDetailsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        match.Rounds = match.Rounds.OrderBy(r => r.NumberInMatch).ToList();

        GetMatchDetailDto matchDto = _mapper.Map<GetMatchDetailDto>(match);

        return Ok(matchDto);
    }

    [HttpGet]
    [Route("{tournamentId:int}/matches/{matchId:int}/available-topics")]
    [Authorize]
    public async Task<IActionResult> GetAvailableTopicsForMatch([FromRoute] int tournamentId, [FromRoute] int matchId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdWithDetailsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        List<int> participantUserIds = match.MatchParticipants
            .Select(mp => mp.TournamentParticipant.ApplicationUserId)
            .ToList();

        List<TournamentTopic> allTournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);

        List<Round> allRoundsInTournament = await _roundRepository.GetAllByTournamentIdAsync(tournamentId);
        HashSet<int> playedTopicIds = allRoundsInTournament.Select(r => r.TopicId).ToHashSet();

        List<TournamentTopic> filteredTopics = [];

        foreach (TournamentTopic tournamentTopic in allTournamentTopics)
        {
            if (playedTopicIds.Contains(tournamentTopic.TopicId))
                continue;

            Topic? topic = await _topicRepository.GetByIdIncludeQuestionsAsync(tournamentTopic.TopicId);
            if (topic is null)
                continue;

            bool participantIsAuthor = topic.TopicAuthors
                .Any(ta => ta.IsAuthor && participantUserIds.Contains(ta.ApplicationUserId));

            if (participantIsAuthor)
                continue;

            filteredTopics.Add(tournamentTopic);
        }

        filteredTopics = filteredTopics.OrderBy(t => t.PriorityIndex).ToList();
        List<GetAvailableTopicDto> availableTopics = _mapper.Map<List<GetAvailableTopicDto>>(filteredTopics);

        return Ok(availableTopics);
    }

    [HttpGet]
    [Route("{tournamentId:int}/topics/{topicId:int}")]
    [Authorize]
    public async Task<IActionResult> GetTournamentTopicById([FromRoute] int tournamentId, [FromRoute] int topicId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        if (!tournamentTopics.Any(tt => tt.TopicId == topicId))
            return NotFound("Topic is not part of this tournament.");

        Topic? topic = await _topicRepository.GetByIdIncludeQuestionsAsync(topicId);
        if (topic is null)
            return NotFound();

        GetTopicDto topicDto = _mapper.Map<GetTopicDto>(topic);

        return Ok(topicDto);
    }

    [HttpPost]
    [Route("{tournamentId:int}/matches/{matchId:int}/rounds")]
    [Authorize]
    public async Task<IActionResult> CreateRound(
        [FromRoute] int tournamentId,
        [FromRoute] int matchId,
        [FromBody] CreateRoundWithAnswersDto createRoundDto)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdWithDetailsAsync(matchId);
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

        Round round = new()
        {
            NumberInMatch = createRoundDto.NumberInMatch,
            TopicId = createRoundDto.TopicId,
            Topic = topic,
            MatchId = matchId,
            Match = match
        };

        List<RoundAnswer> answers = _mapper.Map<List<RoundAnswer>>(createRoundDto.Answers);
        foreach (var answer in answers)
        {
            answer.Round = round;
        }

        await _roundRepository.CreateWithAnswersAsync(round, answers);

        return await GetMatchById(tournamentId, matchId);
    }

    [HttpPut]
    [Route("{tournamentId:int}/matches/{matchId:int}/rounds/{roundNumber:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateRound(
        [FromRoute] int tournamentId,
        [FromRoute] int matchId,
        [FromRoute] int roundNumber,
        [FromBody] CreateRoundWithAnswersDto updateRoundDto)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdWithDetailsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        if (updateRoundDto.NumberInMatch != roundNumber)
            return BadRequest("Round number in URL must match round number in body.");

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is null)
            return NotFound($"Round {roundNumber} not found for this match.");

        if (existingRound.TopicId != updateRoundDto.TopicId)
        {
            await _roundRepository.DeleteAsync(existingRound.Id);

            Topic? topic = await _topicRepository.GetByIdAsync(updateRoundDto.TopicId);
            if (topic is null)
                return BadRequest("Topic not found.");

            Round newRound = new()
            {
                NumberInMatch = roundNumber,
                TopicId = updateRoundDto.TopicId,
                Topic = topic,
                MatchId = matchId,
                Match = match
            };

            List<RoundAnswer> newAnswers = _mapper.Map<List<RoundAnswer>>(updateRoundDto.Answers);
            foreach (var answer in newAnswers)
            {
                answer.Round = newRound;
            }

            await _roundRepository.CreateWithAnswersAsync(newRound, newAnswers);
        }
        else
        {
            await _roundRepository.DeleteAnswersByRoundIdAsync(existingRound.Id);

            List<RoundAnswer> newAnswers = _mapper.Map<List<RoundAnswer>>(updateRoundDto.Answers);
            foreach (var answer in newAnswers)
            {
                answer.RoundId = existingRound.Id;
                answer.Round = existingRound;
            }

            await _roundRepository.CreateAnswersAsync(newAnswers);
        }

        return await GetMatchById(tournamentId, matchId);
    }
}
