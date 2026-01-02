using Cbo.API.Authorization;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/matches")]
    [Authorize]
    public async Task<IActionResult> GetAllMatches([FromRoute] Guid tournamentId)
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
    [Route("{tournamentId:guid}/matches/{matchId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMatchById([FromRoute] Guid tournamentId, [FromRoute] Guid matchId)
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
    [Route("{tournamentId:guid}/matches/{matchId:guid}/available-topics")]
    [Authorize]
    public async Task<IActionResult> GetAvailableTopicsForMatch([FromRoute] Guid tournamentId, [FromRoute] Guid matchId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ViewAllTopics);
        if (!authResult.Succeeded)
            return NotFound();

        Match? match = await _matchRepository.GetByIdWithParticipantsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return NotFound();

        HashSet<Guid> participantUserIds = match.MatchParticipants
            .Select(mp => mp.TournamentParticipant.ApplicationUserId)
            .ToHashSet();

        List<TournamentTopic> allTournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdWithAuthorsAsync(tournamentId);

        List<Round> allRoundsInTournament = await _roundRepository.GetAllByTournamentIdAsync(tournamentId);
        HashSet<Guid> playedTopicIds = allRoundsInTournament.Select(r => r.TopicId).ToHashSet();

        List<TournamentTopic> filteredTopics = allTournamentTopics
            .Where(tt => !playedTopicIds.Contains(tt.TopicId))
            .Where(tt => tt.Topic?.TopicAuthors is null ||
                         !tt.Topic.TopicAuthors.Any(ta => ta.IsAuthor && participantUserIds.Contains(ta.ApplicationUserId)))
            .OrderBy(tt => tt.PriorityIndex)
            .ToList();

        List<GetAvailableTopicDto> availableTopics = _mapper.Map<List<GetAvailableTopicDto>>(filteredTopics);

        return Ok(availableTopics);
    }
}
