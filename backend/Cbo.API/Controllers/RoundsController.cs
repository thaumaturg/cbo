using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoundsController : ControllerBase
{
    private readonly IRoundRepository _roundRepository;
    private readonly IMapper _mapper;

    public RoundsController(
        IRoundRepository roundRepository,
        IMapper mapper)
    {
        _roundRepository = roundRepository;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        List<Round> roundsDomain = await _roundRepository.GetAllAsync();

        List<GetRoundDto> roundsDto = _mapper.Map<List<GetRoundDto>>(roundsDomain);

        return Ok(roundsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        Round? roundDomain = await _roundRepository.GetByIdAsync(id);

        if (roundDomain is null)
            return NotFound();

        GetRoundDto getRoundDto = _mapper.Map<GetRoundDto>(roundDomain);

        return Ok(getRoundDto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateRoundDto createRoundDto)
    {
        Round roundDomain = _mapper.Map<Round>(createRoundDto);

        roundDomain = await _roundRepository.CreateAsync(roundDomain);

        Round? round = await _roundRepository.GetByIdAsync(roundDomain.Id);

        if (round is null)
            return BadRequest();

        GetRoundDto getRoundDto = _mapper.Map<GetRoundDto>(round);

        return CreatedAtAction(nameof(GetById), new { id = roundDomain.Id }, getRoundDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRoundDto updateRoundDto)
    {
        Round? roundDomain = _mapper.Map<Round>(updateRoundDto);

        roundDomain = await _roundRepository.UpdateAsync(id, roundDomain);

        if (roundDomain is null)
            return NotFound();

        GetRoundDto getRoundDto = _mapper.Map<GetRoundDto>(roundDomain);

        return Ok(getRoundDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Round? roundDomain = await _roundRepository.DeleteAsync(id);

        if (roundDomain is null)
            return NotFound();

        GetRoundDto getRoundDto = _mapper.Map<GetRoundDto>(roundDomain);

        return Ok(getRoundDto);
    }
}
