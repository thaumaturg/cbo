using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            return Problem(detail: "Cannot add a creator. Tournament can only have one creator.", statusCode: StatusCodes.Status400BadRequest);

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
                return Problem(detail: $"Tournament can have at most {DefaultSettings.OrganizersPerTournamentMax} organizers.", statusCode: StatusCodes.Status400BadRequest);
        }

        ApplicationUser? user = await _userManager.FindByNameAsync(createParticipantDto.Username);
        if (user is null)
            return Problem(detail: $"User with username '{createParticipantDto.Username}' not found.", statusCode: StatusCodes.Status404NotFound);

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(user.Id, tournamentId);
        if (existingParticipant is not null)
            return Problem(detail: $"User '{createParticipantDto.Username}' is already a participant in this tournament.", statusCode: StatusCodes.Status409Conflict);

        TournamentParticipant participantDomain = createParticipantDto.ToNewParticipant(tournamentId, user.Id);

        if (createParticipantDto.Role == TournamentParticipantRole.Player)
            participantDomain.Seed = await GetNextPlayerSeedAsync(tournamentId);

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
            return Problem(detail: "Cannot promote to a creator role.", statusCode: StatusCodes.Status400BadRequest);

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
                return Problem(detail: $"Tournament can have at most {DefaultSettings.OrganizersPerTournamentMax} organizers.", statusCode: StatusCodes.Status400BadRequest);
        }

        bool becomesPlayer = updateParticipantDto.Role == TournamentParticipantRole.Player && existingParticipant.Role != TournamentParticipantRole.Player;
        bool leavesPlayerRole = updateParticipantDto.Role != TournamentParticipantRole.Player && existingParticipant.Role == TournamentParticipantRole.Player;

        int? seed = existingParticipant.Seed;
        if (becomesPlayer)
            seed = await GetNextPlayerSeedAsync(tournamentId);
        else if (updateParticipantDto.Role != TournamentParticipantRole.Player)
            seed = null;

        var updateParameters = new Repositories.UpdateTournamentParticipantParameters
        {
            Role = updateParticipantDto.Role,
            Seed = seed
        };

        TournamentParticipant? updatedParticipant = await _participantsRepository.UpdateAsync(id, updateParameters);

        if (updatedParticipant is null)
            return NotFound();

        if (leavesPlayerRole)
            await CompactPlayerSeedsAsync(tournamentId);

        GetTournamentParticipantDto participantDto = updatedParticipant.ToGetDto();

        return Ok(participantDto);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/participants/seeding")]
    [Authorize]
    public async Task<IActionResult> UpdateParticipantsSeeding([FromRoute] Guid tournamentId, [FromBody] UpdateParticipantsSeedingDto seedingDto)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.ManageParticipants);
        if (!authResult.Succeeded)
            return NotFound();

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return Problem(detail: "Seeding can only be changed during the Preparations stage.", statusCode: StatusCodes.Status400BadRequest);

        List<TournamentParticipant> players = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Player);

        HashSet<Guid> playerIds = players.Select(p => p.Id).ToHashSet();
        if (seedingDto.OrderedParticipantIds.Count != playerIds.Count || !playerIds.SetEquals(seedingDto.OrderedParticipantIds))
            return Problem(detail: "Seeding must contain each player of the tournament exactly once.", statusCode: StatusCodes.Status400BadRequest);

        List<TournamentParticipant> reseededPlayers = await _participantsRepository.UpdateSeedsAsync(tournamentId, seedingDto.OrderedParticipantIds);

        List<GetTournamentParticipantDto> playersDto = reseededPlayers.Select(p => p.ToGetDto()).ToList();

        return Ok(playersDto);
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

        TournamentParticipant? participantDomain;
        try
        {
            participantDomain = await _participantsRepository.DeleteAsync(id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Problem(detail: "This participant has already played in a match and cannot be removed.", statusCode: StatusCodes.Status409Conflict);
        }

        if (participantDomain is null)
            return NotFound();

        if (participantDomain.Role == TournamentParticipantRole.Player)
            await CompactPlayerSeedsAsync(tournamentId);

        return NoContent();
    }

    /// <summary>
    /// Returns the seed for a newly added player: one past the highest seed currently taken.
    /// </summary>
    private async Task<int> GetNextPlayerSeedAsync(Guid tournamentId)
    {
        List<TournamentParticipant> players = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Player);

        return players.Count == 0 ? 1 : players.Max(p => p.Seed ?? 0) + 1;
    }

    /// <summary>
    /// Renumbers player seeds to a contiguous 1..N sequence, preserving the current order.
    /// Used after a player leaves the seeding (removed or demoted to another role).
    /// </summary>
    private async Task CompactPlayerSeedsAsync(Guid tournamentId)
    {
        List<TournamentParticipant> players = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Player);

        List<Guid> orderedIds = players
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.Id)
            .Select(p => p.Id)
            .ToList();

        await _participantsRepository.UpdateSeedsAsync(tournamentId, orderedIds);
    }
}
