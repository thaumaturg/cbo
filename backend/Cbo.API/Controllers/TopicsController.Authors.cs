using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TopicsController
{
    [HttpGet]
    [Route("{topicId:guid}/authors")]
    [Authorize]
    public async Task<IActionResult> GetAllAuthors([FromRoute] Guid topicId)
    {
        Result<List<GetTopicAuthorDto>> result = await _topicService.GetAuthorsAsync(topicId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{topicId:guid}/authors/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAuthorById([FromRoute] Guid topicId, [FromRoute] Guid id)
    {
        Result<GetTopicAuthorDto> result = await _topicService.GetAuthorAsync(topicId, id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("{topicId:guid}/authors")]
    [Authorize]
    public async Task<IActionResult> CreateAuthor([FromRoute] Guid topicId, [FromBody] CreateTopicAuthorDto createAuthorDto)
    {
        Result<GetTopicAuthorDto> result = await _topicService.CreateAuthorAsync(topicId, createAuthorDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        GetTopicAuthorDto authorDto = result.Value;
        return CreatedAtAction(nameof(GetAuthorById), new { topicId, id = authorDto.Id }, authorDto);
    }

    [HttpDelete]
    [Route("{topicId:guid}/authors/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAuthor([FromRoute] Guid topicId, [FromRoute] Guid id)
    {
        Result result = await _topicService.DeleteAuthorAsync(topicId, id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
