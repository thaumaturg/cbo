using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.Results;
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
        Result<List<GetTournamentTopicDto>> result = await _tournamentTopicService.GetMyTopicsAsync(tournamentId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/topics/all")]
    [Authorize]
    public async Task<IActionResult> GetAllTopics([FromRoute] Guid tournamentId)
    {
        Result<List<GetTournamentTopicDto>> result = await _tournamentTopicService.GetAllTopicsAsync(tournamentId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/topics")]
    [Authorize]
    public async Task<IActionResult> SetMyTopics([FromRoute] Guid tournamentId, [FromBody] List<UpdateTournamentTopicDto> topicsDto)
    {
        Result<List<GetTournamentTopicDto>> result = await _tournamentTopicService.SetMyTopicsAsync(tournamentId, topicsDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/topics/{topicId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetTournamentTopicById([FromRoute] Guid tournamentId, [FromRoute] Guid topicId)
    {
        Result<GetTopicDto> result = await _tournamentTopicService.GetTopicAsync(tournamentId, topicId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }
}
