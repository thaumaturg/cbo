using AutoMapper;
using Cbo.API.Authorization;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.API.Services;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMapper _mapper;

    public TopicsController(
        ITopicRepository topicRepository,
        ITopicAuthorRepository topicAuthorRepository,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService,
        IMapper mapper)
    {
        _topicRepository = topicRepository;
        _topicAuthorRepository = topicAuthorRepository;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _authorizationService = authorizationService;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

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
        Topic? topicDomain = await _topicRepository.GetByIdIncludeQuestionsAsync(id);

        if (topicDomain is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topicDomain, TopicOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);
        TopicAuthor? topicAuthor = topicDomain.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
        getTopicDto.IsAuthor = topicAuthor?.IsAuthor ?? false;

        return Ok(getTopicDto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTopicDto createTopicDto)
    {
        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

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
        Topic? existingTopic = await _topicRepository.GetByIdAsync(id);
        if (existingTopic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTopic, TopicOperations.Update);
        if (!authResult.Succeeded)
            return NotFound();
        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

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
        Topic? existingTopic = await _topicRepository.GetByIdAsync(id);
        if (existingTopic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTopic, TopicOperations.Delete);
        if (!authResult.Succeeded)
            return NotFound();

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
