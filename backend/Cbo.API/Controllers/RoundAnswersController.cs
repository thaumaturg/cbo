using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoundAnswersController : ControllerBase
{
    private readonly IRoundAnswerRepository _roundAnswerRepository;
    private readonly IMapper _mapper;

    public RoundAnswersController(
        IRoundAnswerRepository roundAnswerRepository,
        IMapper mapper)
    {
        _roundAnswerRepository = roundAnswerRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<RoundAnswer> roundAnswersDomain = await _roundAnswerRepository.GetAllAsync();

        List<GetRoundAnswerDto> roundAnswersDto = _mapper.Map<List<GetRoundAnswerDto>>(roundAnswersDomain);

        return Ok(roundAnswersDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        RoundAnswer? roundAnswerDomain = await _roundAnswerRepository.GetByIdAsync(id);

        if (roundAnswerDomain is null)
            return NotFound();

        GetRoundAnswerDto getRoundAnswerDto = _mapper.Map<GetRoundAnswerDto>(roundAnswerDomain);

        return Ok(getRoundAnswerDto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoundAnswerDto createRoundAnswerDto)
    {
        RoundAnswer roundAnswerDomain = _mapper.Map<RoundAnswer>(createRoundAnswerDto);

        roundAnswerDomain = await _roundAnswerRepository.CreateAsync(roundAnswerDomain);

        RoundAnswer? roundAnswerInclude = await _roundAnswerRepository.GetByIdIncludeAsync(roundAnswerDomain.Id);

        if (roundAnswerInclude is null)
            return BadRequest();

        GetRoundAnswerDto getRoundAnswerDto = _mapper.Map<GetRoundAnswerDto>(roundAnswerInclude);

        return CreatedAtAction(nameof(GetById), new { id = roundAnswerDomain.Id }, getRoundAnswerDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRoundAnswerDto updateRoundAnswerDto)
    {
        RoundAnswer? roundAnswerDomain = _mapper.Map<RoundAnswer>(updateRoundAnswerDto);

        roundAnswerDomain = await _roundAnswerRepository.UpdateAsync(id, roundAnswerDomain);

        if (roundAnswerDomain is null)
            return NotFound();

        GetRoundAnswerDto getRoundAnswerDto = _mapper.Map<GetRoundAnswerDto>(roundAnswerDomain);

        return Ok(getRoundAnswerDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        RoundAnswer? roundAnswerDomain = await _roundAnswerRepository.DeleteAsync(id);

        if (roundAnswerDomain is null)
            return NotFound();

        GetRoundAnswerDto getRoundAnswerDto = _mapper.Map<GetRoundAnswerDto>(roundAnswerDomain);

        return Ok(getRoundAnswerDto);
    }
}
