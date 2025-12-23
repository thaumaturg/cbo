using AutoMapper;
using Cbo.API.Authorization;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class TournamentsController : ControllerBase
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentParticipantsRepository _participantsRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMatchGenerationService _matchGenerationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMapper _mapper;

    public TournamentsController(
        ITournamentRepository tournamentRepository,
        ITournamentParticipantsRepository participantsRepository,
        ITournamentTopicRepository tournamentTopicRepository,
        ITopicRepository topicRepository,
        IMatchRepository matchRepository,
        ICurrentUserService currentUserService,
        IMatchGenerationService matchGenerationService,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _participantsRepository = participantsRepository;
        _tournamentTopicRepository = tournamentTopicRepository;
        _topicRepository = topicRepository;
        _matchRepository = matchRepository;
        _currentUserService = currentUserService;
        _matchGenerationService = matchGenerationService;
        _userManager = userManager;
        _authorizationService = authorizationService;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        List<Tournament> tournamentsDomain = await _tournamentRepository.GetAllByUserIdAsync(currentUser.Id);

        List<GetTournamentDto> tournamentsDto = _mapper.Map<List<GetTournamentDto>>(tournamentsDomain);

        foreach (GetTournamentDto tournamentDto in tournamentsDto)
        {
            TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, tournamentDto.Id);
            tournamentDto.CurrentUserRole = participant?.Role;
        }

        return Ok(tournamentsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.GetByIdAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournamentDomain, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, id);
        tournamentDto.CurrentUserRole = participant?.Role;

        return Ok(tournamentDto);
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto createTournamentDto)
    {
        if (createTournamentDto.PlayersPerTournament.HasValue &&
            createTournamentDto.PlayersPerTournament.Value != DefaultSettings.PlayersPerTournament)
        {
            return BadRequest($"Only {DefaultSettings.PlayersPerTournament} players per tournament is currently supported.");
        }

        ApplicationUser creator = await _currentUserService.GetRequiredCurrentUserAsync();

        Tournament tournamentDomain = _mapper.Map<Tournament>(createTournamentDto);

        tournamentDomain.TournamentParticipants.Add(new TournamentParticipant
        {
            Role = TournamentParticipantRole.Creator,
            PointsSum = null,
            ApplicationUserId = creator.Id,
        });

        tournamentDomain = await _tournamentRepository.CreateAsync(tournamentDomain);

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return CreatedAtAction(nameof(GetById), new { id = tournamentDomain.Id }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTournamentDto updateTournamentDto)
    {
        if (updateTournamentDto.PlayersPerTournament.HasValue &&
            updateTournamentDto.PlayersPerTournament.Value != DefaultSettings.PlayersPerTournament)
        {
            return BadRequest($"Only {DefaultSettings.PlayersPerTournament} players per tournament is currently supported.");
        }

        Tournament? existingTournament = await _tournamentRepository.GetByIdAsync(id);
        if (existingTournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTournament, TournamentOperations.Update);
        if (!authResult.Succeeded)
            return NotFound();

        Tournament? tournamentDomain = _mapper.Map<Tournament>(updateTournamentDto);

        tournamentDomain = await _tournamentRepository.UpdateAsync(id, tournamentDomain);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Tournament? existingTournament = await _tournamentRepository.GetByIdAsync(id);
        if (existingTournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTournament, TournamentOperations.Delete);
        if (!authResult.Succeeded)
            return NotFound();

        Tournament? tournamentDomain = await _tournamentRepository.DeleteAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }

    [HttpPatch]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> AdvanceStage([FromRoute] int id, [FromBody] UpdateTournamentStageDto advanceStageDto)
    {
        if (advanceStageDto.Stage != TournamentStage.Qualifications)
            return BadRequest("Only advancing to Qualifications stage is currently supported.");

        Tournament? existingTournament = await _tournamentRepository.GetByIdAsync(id);
        if (existingTournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTournament, TournamentOperations.AdvanceStage);
        if (!authResult.Succeeded)
            return NotFound();

        if (existingTournament.CurrentStage != TournamentStage.Preparations)
            return BadRequest("Tournament can only advance to Qualifications from Preparations stage.");

        List<TournamentParticipant> allParticipants = await _participantsRepository.GetAllByTournamentIdAsync(id);

        List<TournamentParticipant> players = allParticipants
            .Where(p => p.Role == TournamentParticipantRole.Player)
            .OrderBy(p => p.Id)
            .ToList();

        if (players.Count != existingTournament.PlayersPerTournament)
            return BadRequest($"Tournament requires exactly {existingTournament.PlayersPerTournament} players. Currently has {players.Count}.");

        List<TournamentTopic> allTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(id);

        foreach (TournamentParticipant participant in allParticipants)
        {
            int topicCount = allTopics.Count(t => t.TournamentParticipantId == participant.Id);

            if (participant.Role == TournamentParticipantRole.Player)
            {
                if (topicCount < existingTournament.TopicsPerParticipantMin)
                    return BadRequest($"Player '{participant.ApplicationUser?.UserName ?? participant.Id.ToString()}' has {topicCount} topics, but needs at least {existingTournament.TopicsPerParticipantMin}.");

                if (topicCount > existingTournament.TopicsPerParticipantMax)
                    return BadRequest($"Player '{participant.ApplicationUser?.UserName ?? participant.Id.ToString()}' has {topicCount} topics, but maximum allowed is {existingTournament.TopicsPerParticipantMax}.");
            }
            else
            {
                if (topicCount > existingTournament.TopicsPerParticipantMax)
                    return BadRequest($"Participant '{participant.ApplicationUser?.UserName ?? participant.Id.ToString()}' has {topicCount} topics, but maximum allowed is {existingTournament.TopicsPerParticipantMax}.");
            }
        }

        List<Match> matches = _matchGenerationService.GenerateQualificationMatches(existingTournament, players);
        await _matchRepository.CreateBulkAsync(matches);

        Tournament? tournamentDomain = await _tournamentRepository.UpdateStageAsync(id, advanceStageDto.Stage);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }
}
