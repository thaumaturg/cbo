using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/participants")]
    [Authorize]
    public async Task<IActionResult> GetAllParticipants(
        [FromRoute] Guid tournamentId,
        [FromQuery] TournamentParticipantRole? role = null)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        List<TournamentParticipant> participantsDomain = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, role);
        List<GetTournamentParticipantDto> participantsDto = participantsDomain.Select(p => p.ToGetDto()).ToList();

        return Ok(participantsDto);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/participants/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetParticipantById([FromRoute] Guid tournamentId, [FromRoute] Guid id)
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

        GetTournamentParticipantDto participantDto = participantDomain.ToGetDto();

        return Ok(participantDto);
    }

    [HttpPost]
    [Route("{tournamentId:guid}/participants")]
    [Authorize]
    public async Task<IActionResult> CreateParticipant([FromRoute] Guid tournamentId, [FromBody] CreateTournamentParticipantDto createParticipantDto)
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

        TournamentParticipant participantDomain = createParticipantDto.ToNewParticipant(tournamentId, user.Id);

        participantDomain = await _participantsRepository.CreateAsync(participantDomain);

        GetTournamentParticipantDto participantDto = participantDomain.ToGetDto();

        return CreatedAtAction(nameof(GetParticipantById), new { tournamentId, id = participantDomain.Id }, participantDto);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/participants/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateParticipant([FromRoute] Guid tournamentId, [FromRoute] Guid id, [FromBody] UpdateTournamentParticipantDto updateParticipantDto)
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

        var updateParameters = new Repositories.UpdateTournamentParticipantParameters
        {
            Role = updateParticipantDto.Role
        };

        TournamentParticipant? updatedParticipant = await _participantsRepository.UpdateAsync(id, updateParameters);

        if (updatedParticipant is null)
            return NotFound();

        GetTournamentParticipantDto participantDto = updatedParticipant.ToGetDto();

        return Ok(participantDto);
    }

    [HttpDelete]
    [Route("{tournamentId:guid}/participants/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteParticipant([FromRoute] Guid tournamentId, [FromRoute] Guid id)
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

        return NoContent();
    }
}
