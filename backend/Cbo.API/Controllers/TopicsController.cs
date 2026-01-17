using Cbo.API.Authorization;
using Cbo.API.Mappings;
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
public partial class TopicsController : ControllerBase
{
    private readonly ITopicRepository _topicRepository;
    private readonly ITopicAuthorRepository _topicAuthorRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;

    public TopicsController(
        ITopicRepository topicRepository,
        ITopicAuthorRepository topicAuthorRepository,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        _topicRepository = topicRepository;
        _topicAuthorRepository = topicAuthorRepository;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        List<Topic> topicsDomain = await _topicRepository.GetAllByUserIdAsync(currentUser.Id);

        List<GetTopicDto> topicsDto = topicsDomain.Select(topic =>
        {
            TopicAuthor? topicAuthor = topic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
            bool isAuthor = topicAuthor?.IsAuthor ?? false;
            bool isPlayed = topic.Rounds.Count > 0;
            return topic.ToGetDto(isPlayed, isAuthor);
        }).ToList();

        return Ok(topicsDto);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        Topic? topicDomain = await _topicRepository.GetByIdIncludeQuestionsAsync(id);

        if (topicDomain is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, topicDomain, TopicOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        TopicAuthor? topicAuthor = topicDomain.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
        bool isAuthor = topicAuthor?.IsAuthor ?? false;
        bool isPlayed = topicDomain.Rounds.Count > 0;

        return Ok(topicDomain.ToGetDto(isPlayed, isAuthor));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTopicDto createTopicDto)
    {
        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        Topic topicDomain = createTopicDto.ToDomain();

        // Map and add questions
        foreach (CreateQuestionDto questionDto in createTopicDto.Questions)
        {
            topicDomain.Questions.Add(questionDto.ToDomain(Guid.Empty));
        }

        // Create TopicAuthor entry for the current user as owner
        topicDomain.TopicAuthors.Add(new TopicAuthor
        {
            IsOwner = true,
            IsAuthor = createTopicDto.IsAuthor,
            ApplicationUserId = currentUser.Id,
            TopicId = Guid.Empty
        });

        topicDomain = await _topicRepository.CreateAsync(topicDomain);

        Topic? topicIncludeQuestions = await _topicRepository.GetByIdIncludeQuestionsAsync(topicDomain.Id);

        if (topicIncludeQuestions is null)
            return BadRequest();

        GetTopicDto getTopicDto = topicIncludeQuestions.ToGetDto(isPlayed: false, isAuthor: createTopicDto.IsAuthor);

        return CreatedAtAction(nameof(GetById), new { id = topicDomain.Id }, getTopicDto);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTopicDto updateTopicDto)
    {
        Topic? existingTopic = await _topicRepository.GetByIdAsync(id);
        if (existingTopic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTopic, TopicOperations.Update);
        if (!authResult.Succeeded)
            return NotFound();

        if (existingTopic.Rounds.Count > 0)
            return BadRequest("Cannot edit a topic that has already been played in a round.");

        ApplicationUser currentUser = await _currentUserService.GetRequiredCurrentUserAsync();

        var updateParameters = new UpdateTopicParameters
        {
            Title = updateTopicDto.Title,
            Description = updateTopicDto.Description,
            IsAuthor = updateTopicDto.IsAuthor,
            Questions = updateTopicDto.Questions.Select(q => new UpdateQuestionParameters
            {
                Id = q.Id,
                QuestionNumber = q.QuestionNumber,
                CostPositive = q.CostPositive,
                CostNegative = q.CostNegative,
                Text = q.Text,
                Answer = q.Answer,
                Comment = q.Comment
            }).ToList()
        };

        Topic? updatedTopic;
        try
        {
            updatedTopic = await _topicRepository.UpdateAsync(id, updateParameters, currentUser.Id);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (updatedTopic is null)
            return NotFound();

        TopicAuthor? topicAuthor = updatedTopic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUser.Id);
        bool isAuthor = topicAuthor?.IsAuthor ?? false;

        return Ok(updatedTopic.ToGetDto(isPlayed: false, isAuthor));
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        Topic? existingTopic = await _topicRepository.GetByIdAsync(id);
        if (existingTopic is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, existingTopic, TopicOperations.Delete);
        if (!authResult.Succeeded)
            return NotFound();

        if (existingTopic.Rounds.Count > 0)
            return BadRequest("Cannot delete a topic that has already been played in a round.");

        Topic? topicDomain = await _topicRepository.DeleteAsync(id);

        if (topicDomain is null)
            return NotFound();

        return NoContent();
    }
}
