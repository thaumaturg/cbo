using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TournamentsController : ControllerBase
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly IMapper _mapper;

    private TournamentService _tournamentService;

    public TournamentsController(
        ITournamentRepository tournamentRepository,
        TournamentService tournamentService,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _tournamentService = tournamentService;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Reader")]
    public async Task<IActionResult> GetAll()
    {
        List<Tournament> tournamentsDomain = await _tournamentRepository.GetAllAsync();

        List<GetTournamentDto> tournamentsDto = _mapper.Map<List<GetTournamentDto>>(tournamentsDomain);

        return Ok(tournamentsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    [Authorize(Roles = "Reader")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.GetByIdAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }


    [HttpPost]
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto createTournamentDto)
    {
        int tid = await _tournamentService.createTournamentWithParticipants(createTournamentDto);

        Tournament? tournamentIncludeSettings = await _tournamentRepository.GetByIdIncludeSettingsAsync(tid);

        if (tournamentIncludeSettings is null)
            return BadRequest();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentIncludeSettings);

        return CreatedAtAction(nameof(GetById), new { id = tid }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTournamentDto updateTournamentDto)
    {
        Tournament? tournamentDomain = _mapper.Map<Tournament>(updateTournamentDto);

        tournamentDomain = await _tournamentRepository.UpdateAsync(id, tournamentDomain);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = "Writer")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.DeleteAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }
}
