﻿using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TopicsController : ControllerBase
{
    private readonly ITopicRepository _topicRepository;
    private readonly IMapper _mapper;

    public TopicsController(
        ITopicRepository topicRepository,
        IMapper mapper)
    {
        _topicRepository = topicRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<Topic> topicsDomain = await _topicRepository.GetAllAsync();

        List<GetTopicDto> topicsDto = _mapper.Map<List<GetTopicDto>>(topicsDomain);

        return Ok(topicsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        Topic? topicDomain = await _topicRepository.GetByIdAsync(id);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);

        return Ok(getTopicDto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTopicDto createTopicDto)
    {
        int questionNumber = 1;
        foreach (CreateQuestionDto question in createTopicDto.Questions)
        {
            question.QuestionNumber = questionNumber;
            question.CostPositive ??= questionNumber * 10;
            question.CostNegative ??= questionNumber * 10;
            questionNumber++;
        }

        Topic topicDomain = _mapper.Map<Topic>(createTopicDto);

        topicDomain = await _topicRepository.CreateAsync(topicDomain);

        Topic? topicIncludeQuestions = await _topicRepository.GetByIdIncludeQuestionsAsync(topicDomain.Id);

        if (topicIncludeQuestions is null)
            return BadRequest();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicIncludeQuestions);

        return CreatedAtAction(nameof(GetById), new { id = topicDomain.Id }, getTopicDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTopicDto updateTopicDto)
    {
        Topic? topicDomain = _mapper.Map<Topic>(updateTopicDto);

        topicDomain = await _topicRepository.UpdateAsync(id, topicDomain);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);

        return Ok(getTopicDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Topic? topicDomain = await _topicRepository.DeleteAsync(id);

        if (topicDomain is null)
            return NotFound();

        GetTopicDto getTopicDto = _mapper.Map<GetTopicDto>(topicDomain);

        return Ok(getTopicDto);
    }
}
