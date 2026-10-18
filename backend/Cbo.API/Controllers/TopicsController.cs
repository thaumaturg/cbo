using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class TopicsController(ITopicService topicService) : ControllerBase
{
    private readonly ITopicService _topicService = topicService;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        List<GetTopicDto> topicsDto = await _topicService.GetTopicsAsync();

        return Ok(topicsDto);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        Result<GetTopicDto> result = await _topicService.GetTopicAsync(id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTopicDto createTopicDto)
    {
        Result<GetTopicDto> result = await _topicService.CreateTopicAsync(createTopicDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        GetTopicDto topicDto = result.Value;
        return CreatedAtAction(nameof(GetById), new { id = topicDto.Id }, topicDto);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTopicDto updateTopicDto)
    {
        Result<GetTopicDto> result = await _topicService.UpdateTopicAsync(id, updateTopicDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        Result result = await _topicService.DeleteTopicAsync(id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
