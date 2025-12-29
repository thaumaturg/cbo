using Cbo.API.Authorization;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/topics")]
    [Authorize]
    public async Task<IActionResult> GetMyTopics([FromRoute] Guid tournamentId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, tournamentId);

        List<TournamentTopic> topicsDomain = await _tournamentTopicRepository.GetAllByParticipantIdAsync(tournamentId, participant!.Id);
        List<GetTournamentTopicDto> topicsDto = _mapper.Map<List<GetTournamentTopicDto>>(topicsDomain);

        return Ok(topicsDto);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/topics/all")]
    [Authorize]
    public async Task<IActionResult> GetAllTopics([FromRoute] Guid tournamentId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ViewAllTopics);
        if (!authResult.Succeeded)
            return NotFound();

        List<TournamentTopic> topicsDomain = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        List<GetTournamentTopicDto> topicsDto = _mapper.Map<List<GetTournamentTopicDto>>(topicsDomain);

        return Ok(topicsDto);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/topics")]
    [Authorize]
    public async Task<IActionResult> SetMyTopics([FromRoute] Guid tournamentId, [FromBody] List<UpdateTournamentTopicDto> topicsDto)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, tournamentId);

        if (topicsDto.Count < tournament.TopicsPerParticipantMin)
            return BadRequest($"Must assign at least {tournament.TopicsPerParticipantMin} topics.");

        if (topicsDto.Count > tournament.TopicsPerParticipantMax)
            return BadRequest($"Cannot assign more than {tournament.TopicsPerParticipantMax} topics.");

        List<TournamentTopic> topicsDomain = [];
        HashSet<Guid> seenTopicIds = [];

        foreach (UpdateTournamentTopicDto dto in topicsDto)
        {
            if (!seenTopicIds.Add(dto.TopicId))
                return BadRequest($"Duplicate topic ID {dto.TopicId} in the list.");

            Topic? topic = await _topicRepository.GetByIdAsync(dto.TopicId);
            if (topic is null)
                return BadRequest($"Topic with ID {dto.TopicId} not found.");

            AuthorizationResult topicAuthResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.Read);
            if (!topicAuthResult.Succeeded)
                return BadRequest($"You must be the owner of topic with ID {dto.TopicId} to assign it.");

            TournamentTopic topicDomain = _mapper.Map<TournamentTopic>(dto);
            topicDomain.TournamentId = tournamentId;
            topicDomain.TournamentParticipantId = participant!.Id;

            topicsDomain.Add(topicDomain);
        }

        List<TournamentTopic> result = await _tournamentTopicRepository.SetTopicsForParticipantAsync(tournamentId, participant!.Id, topicsDomain);
        List<GetTournamentTopicDto> resultDto = _mapper.Map<List<GetTournamentTopicDto>>(result);

        return Ok(resultDto);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/topics/{topicId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetTournamentTopicById([FromRoute] Guid tournamentId, [FromRoute] Guid topicId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ViewAllTopics);
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
}
