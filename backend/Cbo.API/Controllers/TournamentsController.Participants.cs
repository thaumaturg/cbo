using Cbo.API.Authorization;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
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
}
