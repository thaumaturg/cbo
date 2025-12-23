using Cbo.API.Authorization;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TopicsController
{
    [HttpGet]
    [Route("{topicId:int}/authors")]
    [Authorize]
    public async Task<IActionResult> GetAllAuthors([FromRoute] int topicId)
    {
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        List<TopicAuthor> authorsDomain = await _topicAuthorRepository.GetAllByTopicIdAsync(topicId);
        List<GetTopicAuthorDto> authorsDto = _mapper.Map<List<GetTopicAuthorDto>>(authorsDomain);

        return Ok(authorsDto);
    }

    [HttpGet]
    [Route("{topicId:int}/authors/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetAuthorById([FromRoute] int topicId, [FromRoute] int id)
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

        GetTopicAuthorDto authorDto = _mapper.Map<GetTopicAuthorDto>(authorDomain);

        return Ok(authorDto);
    }

    [HttpPost]
    [Route("{topicId:int}/authors")]
    [Authorize]
    public async Task<IActionResult> CreateAuthor([FromRoute] int topicId, [FromBody] CreateTopicAuthorDto createAuthorDto)
    {
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        ApplicationUser? user = await _userManager.FindByNameAsync(createAuthorDto.Username);
        if (user is null)
            return NotFound($"User with username '{createAuthorDto.Username}' not found.");

        TopicAuthor? existingAuthor = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(user.Id, topicId);
        if (existingAuthor is not null)
            return Conflict($"User '{createAuthorDto.Username}' is already an author of this topic.");

        TopicAuthor authorDomain = new TopicAuthor
        {
            IsOwner = false,
            IsAuthor = true,
            TopicId = topicId,
            ApplicationUserId = user.Id,
            Topic = topic,
            ApplicationUser = user
        };

        authorDomain = await _topicAuthorRepository.CreateAsync(authorDomain);

        GetTopicAuthorDto authorDto = _mapper.Map<GetTopicAuthorDto>(authorDomain);

        return CreatedAtAction(nameof(GetAuthorById), new { topicId, id = authorDomain.Id }, authorDto);
    }

    [HttpDelete]
    [Route("{topicId:int}/authors/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteAuthor([FromRoute] int topicId, [FromRoute] int id)
    {
        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topic, TopicOperations.ManageAuthors);
        if (!authResult.Succeeded)
            return NotFound();

        TopicAuthor? existingAuthor = await _topicAuthorRepository.GetByAuthorIdAndTopicIdAsync(id, topicId);
        if (existingAuthor is null)
            return NotFound();

        if (existingAuthor.IsOwner)
            return BadRequest("Cannot remove the owner from the authors list.");

        TopicAuthor? authorDomain = await _topicAuthorRepository.DeleteAsync(id);

        if (authorDomain is null)
            return NotFound();

        GetTopicAuthorDto authorDto = _mapper.Map<GetTopicAuthorDto>(authorDomain);

        return Ok(authorDto);
    }
}
