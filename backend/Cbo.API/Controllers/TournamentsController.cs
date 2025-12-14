using AutoMapper;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
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
    private readonly ITopicAuthorRepository _topicAuthorRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public TournamentsController(
        ITournamentRepository tournamentRepository,
        ITournamentParticipantsRepository participantsRepository,
        ITournamentTopicRepository tournamentTopicRepository,
        ITopicAuthorRepository topicAuthorRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _participantsRepository = participantsRepository;
        _tournamentTopicRepository = tournamentTopicRepository;
        _topicAuthorRepository = topicAuthorRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

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
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Tournament? tournamentDomain = await _tournamentRepository.GetByIdAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, id);
        tournamentDto.CurrentUserRole = participant?.Role;

        return Ok(tournamentDto);
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto createTournamentDto)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? creator = await _userManager.FindByNameAsync(username);
        if (creator is null)
            return Unauthorized("User not found in the system.");

        Tournament tournamentDomain = _mapper.Map<Tournament>(createTournamentDto);

        tournamentDomain.TournamentParticipants.Add(new TournamentParticipant
        {
            Id = 0, // Will be assigned by database
            Role = TournamentParticipantRole.Creator,
            PointsSum = null,
            TournamentId = 0, // Will be set automatically by EF Core
            ApplicationUserId = creator.Id,
            Tournament = null!, // Navigation property, not needed for creation
            ApplicationUser = null! // Navigation property, not needed for creation
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
            return NotFound($"Tournament with ID {tournamentId} not found.");

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
            return NotFound($"Tournament with ID {tournamentId} not found.");

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
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound($"Tournament with ID {tournamentId} not found.");

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
            return BadRequest($"Cannot promote to a creator role.");

        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound($"Tournament with ID {tournamentId} not found.");

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(id, tournamentId);
        if (existingParticipant is null)
            return NotFound();

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
            return NotFound($"Tournament with ID {tournamentId} not found.");

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
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound($"Tournament with ID {tournamentId} not found.");

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, tournamentId);
        if (participant is null)
            return Forbid();

        List<TournamentTopic> topicsDomain = await _tournamentTopicRepository.GetAllByParticipantIdAsync(tournamentId, participant.Id);
        List<GetTournamentTopicDto> topicsDto = _mapper.Map<List<GetTournamentTopicDto>>(topicsDomain);

        return Ok(topicsDto);
    }

    [HttpGet]
    [Route("{tournamentId:int}/topics/all")]
    [Authorize]
    public async Task<IActionResult> GetAllTopics([FromRoute] int tournamentId)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound($"Tournament with ID {tournamentId} not found.");

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, tournamentId);
        if (participant is null)
            return Forbid();

        if (participant.Role != TournamentParticipantRole.Creator && participant.Role != TournamentParticipantRole.Organizer)
            return Forbid();

        List<TournamentTopic> topicsDomain = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        List<GetTournamentTopicDto> topicsDto = _mapper.Map<List<GetTournamentTopicDto>>(topicsDomain);

        return Ok(topicsDto);
    }

    [HttpPut]
    [Route("{tournamentId:int}/topics")]
    [Authorize]
    public async Task<IActionResult> SetMyTopics([FromRoute] int tournamentId, [FromBody] List<UpdateTournamentTopicDto> topicsDto)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound($"Tournament with ID {tournamentId} not found.");

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUser.Id, tournamentId);
        if (participant is null)
            return Forbid();

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

            TopicAuthor? topicAuthor = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(currentUser.Id, dto.TopicId);
            if (topicAuthor is null || !topicAuthor.IsOwner)
                return BadRequest($"You must be the owner of topic with ID {dto.TopicId} to assign it.");

            TournamentTopic topicDomain = _mapper.Map<TournamentTopic>(dto);
            topicDomain.TournamentId = tournamentId;
            topicDomain.TournamentParticipantId = participant.Id;

            topicsDomain.Add(topicDomain);
        }

        List<TournamentTopic> result = await _tournamentTopicRepository.SetTopicsForParticipantAsync(tournamentId, participant.Id, topicsDomain);
        List<GetTournamentTopicDto> resultDto = _mapper.Map<List<GetTournamentTopicDto>>(result);

        return Ok(resultDto);
    }

    #endregion
}
