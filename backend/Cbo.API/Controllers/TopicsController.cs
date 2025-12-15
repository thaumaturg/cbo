using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TopicsController : ControllerBase
{
    private readonly ITopicRepository _topicRepository;
    private readonly ITopicAuthorRepository _topicAuthorRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public TopicsController(
        ITopicRepository topicRepository,
        ITopicAuthorRepository topicAuthorRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _topicRepository = topicRepository;
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

        List<Topic> topicsDomain = await _topicRepository.GetAllByUserIdAsync(currentUser.Id);

        List<GetTopicDto> topicsDto = topicsDomain.Select(topic =>
        {
            GetTopicDto dto = _mapper.Map<GetTopicDto>(topic);
            TopicAuthor? topicAuthor = topic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
            dto.IsAuthor = topicAuthor?.IsAuthor ?? false;
            return dto;
        }).ToList();

        return Ok(topicsDto);
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

        Topic? topicDomain = await _topicRepository.GetByIdIncludeQuestionsAsync(id);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);
        TopicAuthor? topicAuthor = topicDomain.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
        getTopicDto.IsAuthor = topicAuthor?.IsAuthor ?? false;

        return Ok(getTopicDto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTopicDto createTopicDto)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic topicDomain = _mapper.Map<Topic>(createTopicDto);

        // Create TopicAuthor entry for the current user as owner
        topicDomain.TopicAuthors.Add(new TopicAuthor
        {
            IsOwner = true,
            IsAuthor = createTopicDto.IsAuthor,
            ApplicationUserId = currentUser.Id
        });

        topicDomain = await _topicRepository.CreateAsync(topicDomain);

        Topic? topicIncludeQuestions = await _topicRepository.GetByIdIncludeQuestionsAsync(topicDomain.Id);

        if (topicIncludeQuestions is null)
            return BadRequest();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicIncludeQuestions);
        getTopicDto.IsAuthor = createTopicDto.IsAuthor;

        return CreatedAtAction(nameof(GetById), new { id = topicDomain.Id }, getTopicDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTopicDto updateTopicDto)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic topicDomain = _mapper.Map<Topic>(updateTopicDto);
        List<Question> incomingQuestions = _mapper.Map<List<Question>>(updateTopicDto.Questions);

        Topic? updatedTopic;
        try
        {
            updatedTopic = await _topicRepository.UpdateAsync(id, topicDomain, currentUser.Id, updateTopicDto.IsAuthor, incomingQuestions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (updatedTopic is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(updatedTopic);
        TopicAuthor? topicAuthor = updatedTopic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
        getTopicDto.IsAuthor = topicAuthor?.IsAuthor ?? false;

        return Ok(getTopicDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Topic? topicDomain = await _topicRepository.DeleteAsync(id);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);

        return Ok(getTopicDto);
    }

    [HttpGet]
    [Route("{topicId:int}/authors")]
    [Authorize]
    public async Task<IActionResult> GetAllAuthors([FromRoute] int topicId)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound($"Topic with ID {topicId} not found.");

        TopicAuthor? ownerCheck = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(currentUser.Id, topicId);
        if (ownerCheck is null || !ownerCheck.IsOwner)
            return Forbid();

        List<TopicAuthor> authorsDomain = await _topicAuthorRepository.GetAllByTopicIdAsync(topicId);
        List<GetTopicAuthorDto> authorsDto = _mapper.Map<List<GetTopicAuthorDto>>(authorsDomain);

        return Ok(authorsDto);
    }

    [HttpGet]
    [Route("{topicId:int}/authors/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetAuthorById([FromRoute] int topicId, [FromRoute] int id)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound($"Topic with ID {topicId} not found.");

        TopicAuthor? ownerCheck = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(currentUser.Id, topicId);
        if (ownerCheck is null || !ownerCheck.IsOwner)
            return Forbid();

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
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound($"Topic with ID {topicId} not found.");

        TopicAuthor? ownerCheck = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(currentUser.Id, topicId);
        if (ownerCheck is null || !ownerCheck.IsOwner)
            return Forbid();

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
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic? topic = await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return NotFound($"Topic with ID {topicId} not found.");

        TopicAuthor? ownerCheck = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(currentUser.Id, topicId);
        if (ownerCheck is null || !ownerCheck.IsOwner)
            return Forbid();

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
