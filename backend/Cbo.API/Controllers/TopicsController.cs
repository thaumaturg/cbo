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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public TopicsController(
        ITopicRepository topicRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _topicRepository = topicRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Reader")]
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
    [Authorize(Roles = "Reader")]
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
    [Authorize(Roles = "Writer")]
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
            ApplicationUserId = currentUser.Id,
            ApplicationUser = currentUser,
            Topic = topicDomain
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
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTopicDto updateTopicDto)
    {
        string? username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("Unable to identify the current user.");

        ApplicationUser? currentUser = await _userManager.FindByNameAsync(username);
        if (currentUser is null)
            return Unauthorized("User not found in the system.");

        Topic? topicDomain = _mapper.Map<Topic>(updateTopicDto);

        topicDomain = await _topicRepository.UpdateAsync(id, topicDomain, currentUser.Id, updateTopicDto.IsAuthor);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);
        TopicAuthor? topicAuthor = topicDomain.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
        getTopicDto.IsAuthor = topicAuthor?.IsAuthor ?? false;

        return Ok(getTopicDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Topic? topicDomain = await _topicRepository.DeleteAsync(id);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);

        return Ok(getTopicDto);
    }
}
