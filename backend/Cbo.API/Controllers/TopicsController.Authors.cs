using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
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
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        List<TopicAuthor> authorsDomain = await _topicAuthorRepository.GetAllByTopicIdAsync(topicId);
        List<GetTopicAuthorDto> authorsDto = authorsDomain.Select(a => a.ToGetDto()).ToList();

        return Ok(authorsDto);
    }

    [HttpGet]
    [Route("{topicId:guid}/authors/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAuthorById([FromRoute] Guid topicId, [FromRoute] Guid id)
    {
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        TopicAuthor? authorDomain = await _topicAuthorRepository.GetByAuthorIdAndTopicIdAsync(id, topicId);

        if (authorDomain is null)
            return NotFound();

        GetTopicAuthorDto authorDto = authorDomain.ToGetDto();

        return Ok(authorDto);
    }

    [HttpPost]
    [Route("{topicId:guid}/authors")]
    [Authorize]
    public async Task<IActionResult> CreateAuthor([FromRoute] Guid topicId, [FromBody] CreateTopicAuthorDto createAuthorDto)
    {
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        if (topic.Rounds.Count > 0)
            return Problem(detail: "Cannot modify authors of a topic that has already been played in a round.", statusCode: StatusCodes.Status400BadRequest);

        ApplicationUser? user = await _userManager.FindByNameAsync(createAuthorDto.Username);
        if (user is null)
            return Problem(detail: $"User with username '{createAuthorDto.Username}' not found.", statusCode: StatusCodes.Status404NotFound);

        TopicAuthor? existingAuthor = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(user.Id, topicId);
        if (existingAuthor is not null)
            return Problem(detail: $"User '{createAuthorDto.Username}' is already an author of this topic.", statusCode: StatusCodes.Status409Conflict);

        TopicAuthor authorDomain = new TopicAuthor
        {
            IsOwner = false,
            IsAuthor = true,
            TopicId = topicId,
            ApplicationUserId = user.Id
        };

        authorDomain = await _topicAuthorRepository.CreateAsync(authorDomain);

        GetTopicAuthorDto authorDto = authorDomain.ToGetDto();

        return CreatedAtAction(nameof(GetAuthorById), new { topicId, id = authorDomain.Id }, authorDto);
    }

    [HttpDelete]
    [Route("{topicId:guid}/authors/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAuthor([FromRoute] Guid topicId, [FromRoute] Guid id)
    {
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        if (topic.Rounds.Count > 0)
            return Problem(detail: "Cannot modify authors of a topic that has already been played in a round.", statusCode: StatusCodes.Status400BadRequest);

        TopicAuthor? existingAuthor = await _topicAuthorRepository.GetByAuthorIdAndTopicIdAsync(id, topicId);
        if (existingAuthor is null)
            return NotFound();

        if (existingAuthor.IsOwner)
            return Problem(detail: "Cannot remove the owner from the authors list.", statusCode: StatusCodes.Status400BadRequest);

        TopicAuthor? authorDomain = await _topicAuthorRepository.DeleteAsync(id);

        if (authorDomain is null)
            return NotFound();

        return NoContent();
    }
}
