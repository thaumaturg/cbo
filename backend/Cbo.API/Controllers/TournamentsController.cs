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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public TournamentsController(
        ITournamentRepository tournamentRepository,
        ITournamentParticipantsRepository participantsRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _participantsRepository = participantsRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Reader")]
    public async Task<IActionResult> GetAll()
    {
        List<Tournament> tournamentsDomain = await _tournamentRepository.GetAllAsync();

        List<GetTournamentDto> tournamentsDto = _mapper.Map<List<GetTournamentDto>>(tournamentsDomain);

        return Ok(tournamentsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    [Authorize(Roles = "Reader")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.GetByIdAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }


    [HttpPost]
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto createTournamentDto)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized("Unable to identify the current user.");
        }

        ApplicationUser? creator = await _userManager.FindByNameAsync(username);
        if (creator is null)
        {
            return Unauthorized("User not found in the system.");
        }

        Tournament tournamentDomain = _mapper.Map<Tournament>(createTournamentDto);
        tournamentDomain = await _tournamentRepository.CreateAsync(tournamentDomain);

        TournamentParticipant organizerParticipant = new TournamentParticipant
        {
            Id = 0, // Will be assigned by database
            Role = TournamentParticipantRole.Organiser,
            PointsSum = 0,
            TournamentId = tournamentDomain.Id,
            ApplicationUserId = creator.Id,
            Tournament = null!, // Navigation property, not needed for creation
            ApplicationUser = null! // Navigation property, not needed for creation
        };

        await _participantsRepository.CreateAsync(organizerParticipant);

        Tournament? tournamentIncludeSettings = await _tournamentRepository.GetByIdIncludeSettingsAsync(tournamentDomain.Id);

        if (tournamentIncludeSettings is null)
        {
            return StatusCode(500, "Tournament created but could not be retrieved.");
        }

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentIncludeSettings);

        return CreatedAtAction(nameof(GetById), new { id = tournamentDomain.Id }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    [Authorize(Roles = "Writer")]
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
    [Authorize(Roles = "Writer")]
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
    [Authorize(Roles = "Reader")]
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
    [Authorize(Roles = "Reader")]
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
    [Authorize(Roles = "Writer")]
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
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> UpdateParticipant([FromRoute] int tournamentId, [FromRoute] int id, [FromBody] UpdateTournamentParticipantDto updateParticipantDto)
    {
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
    [Authorize(Roles = "Writer")]
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
}
