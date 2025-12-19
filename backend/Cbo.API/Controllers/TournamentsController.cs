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
public class TournamentsController : ControllerBase
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentParticipantsRepository _participantsRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMapper _mapper;

    public TournamentsController(
        ITournamentRepository tournamentRepository,
        ITournamentParticipantsRepository participantsRepository,
        ITournamentTopicRepository tournamentTopicRepository,
        ITopicRepository topicRepository,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _participantsRepository = participantsRepository;
        _tournamentTopicRepository = tournamentTopicRepository;
        _topicRepository = topicRepository;
        _currentUserService = currentUserService;
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

    #region TournamentParticipants

    [HttpGet]
    [Route("{tournamentId:int}/participants")]
    [Authorize]
    public async Task<IActionResult> GetAllParticipants([FromRoute] int tournamentId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        List<TournamentParticipant> participantsDomain = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId);
        List<GetTournamentParticipantDto> participantsDto = _mapper.Map<List<GetTournamentParticipantDto>>(participantsDomain);

        return Ok(participantsDto);
    }

    [HttpGet]
    [Route("{tournamentId:int}/participants/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetParticipantById([FromRoute] int tournamentId, [FromRoute] int id)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        TournamentParticipant? participantDomain = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(id, tournamentId);

        if (participantDomain is null)
            return NotFound();

        GetTournamentParticipantDto participantDto = _mapper.Map<GetTournamentParticipantDto>(participantDomain);

        return Ok(participantDto);
    }

    [HttpPost]
    [Route("{tournamentId:int}/participants")]
    [Authorize]
    public async Task<IActionResult> CreateParticipant([FromRoute] int tournamentId, [FromBody] CreateTournamentParticipantDto createParticipantDto)
    {
        if (createParticipantDto.Role == TournamentParticipantRole.Creator)
            return BadRequest("Cannot add a creator. Tournament can only have one creator.");

        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageParticipants);
        if (!authResult.Succeeded)
            return NotFound();

        if (createParticipantDto.Role == TournamentParticipantRole.Organizer)
        {
            List<TournamentParticipant> allParticipants = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId);
            int organizerCount = allParticipants.Count(p => p.Role == TournamentParticipantRole.Organizer);
            if (organizerCount >= DefaultSettings.OrganizersPerTournamentMax)
                return BadRequest($"Tournament can have at most {DefaultSettings.OrganizersPerTournamentMax} organizers.");
        }

        ApplicationUser? user = await _userManager.FindByNameAsync(createParticipantDto.Username);
        if (user is null)
            return NotFound($"User with username '{createParticipantDto.Username}' not found.");

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(user.Id, tournamentId);
        if (existingParticipant is not null)
            return Conflict($"User '{createParticipantDto.Username}' is already a participant in this tournament.");

        TournamentParticipant participantDomain = _mapper.Map<TournamentParticipant>(createParticipantDto);

        participantDomain.TournamentId = tournamentId;
        participantDomain.ApplicationUserId = user.Id;

        participantDomain = await _participantsRepository.CreateAsync(participantDomain);

        GetTournamentParticipantDto participantDto = _mapper.Map<GetTournamentParticipantDto>(participantDomain);

        return CreatedAtAction(nameof(GetParticipantById), new { tournamentId, id = participantDomain.Id }, participantDto);
    }

    [HttpPut]
    [Route("{tournamentId:int}/participants/{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateParticipant([FromRoute] int tournamentId, [FromRoute] int id, [FromBody] UpdateTournamentParticipantDto updateParticipantDto)
    {
        if (updateParticipantDto.Role == TournamentParticipantRole.Creator)
            return BadRequest("Cannot promote to a creator role.");

        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageParticipants);
        if (!authResult.Succeeded)
            return NotFound();

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(id, tournamentId);
        if (existingParticipant is null)
            return NotFound();

        if (updateParticipantDto.Role == TournamentParticipantRole.Organizer && existingParticipant.Role != TournamentParticipantRole.Organizer)
        {
            List<TournamentParticipant> allParticipants = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId);
            int organizerCount = allParticipants.Count(p => p.Role == TournamentParticipantRole.Organizer);
            if (organizerCount >= DefaultSettings.OrganizersPerTournamentMax)
                return BadRequest($"Tournament can have at most {DefaultSettings.OrganizersPerTournamentMax} organizers.");
        }

        TournamentParticipant? participantDomain = _mapper.Map<TournamentParticipant>(updateParticipantDto);
        participantDomain = await _participantsRepository.UpdateAsync(id, participantDomain!);

        if (participantDomain is null)
            return NotFound();

        GetTournamentParticipantDto participantDto = _mapper.Map<GetTournamentParticipantDto>(participantDomain);

        return Ok(participantDto);
    }

    [HttpDelete]
    [Route("{tournamentId:int}/participants/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteParticipant([FromRoute] int tournamentId, [FromRoute] int id)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageParticipants);
        if (!authResult.Succeeded)
            return NotFound();

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(id, tournamentId);
        if (existingParticipant is null)
            return NotFound();

        TournamentParticipant? participantDomain = await _participantsRepository.DeleteAsync(id);

        if (participantDomain is null)
            return NotFound();

        GetTournamentParticipantDto participantDto = _mapper.Map<GetTournamentParticipantDto>(participantDomain);

        return Ok(participantDto);
    }

    #endregion

    #region TournamentTopics

    [HttpGet]
    [Route("{tournamentId:int}/topics")]
    [Authorize]
    public async Task<IActionResult> GetMyTopics([FromRoute] int tournamentId)
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
    [Route("{tournamentId:int}/topics/all")]
    [Authorize]
    public async Task<IActionResult> GetAllTopics([FromRoute] int tournamentId)
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
    [Route("{tournamentId:int}/topics")]
    [Authorize]
    public async Task<IActionResult> SetMyTopics([FromRoute] int tournamentId, [FromBody] List<UpdateTournamentTopicDto> topicsDto)
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
        HashSet<int> seenTopicIds = [];

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

    #endregion
}
