using AutoMapper;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TournamentsController : ControllerBase
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;

    public TournamentsController(
        ITournamentRepository tournamentRepository,
        ISettingsRepository settingsRepository,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _settingsRepository = settingsRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<Tournament> tournamentsDomain = await _tournamentRepository.GetAllAsync();

        List<GetTournamentDto> tournamentsDto = _mapper.Map<List<GetTournamentDto>>(tournamentsDomain);

        return Ok(tournamentsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.GetByIdAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto createTournamentDto)
    {
        Tournament tournamentDomain = _mapper.Map<Tournament>(createTournamentDto);

        tournamentDomain = await _tournamentRepository.CreateAsync(tournamentDomain);

        var settingsDomain = new Settings
        {
            ParticipantsPerMatch = createTournamentDto.Settings.ParticipantsPerMatch
                ?? DefaultSettings.TournamentSettings["ParticipantsPerMatch"],
            ParticipantsPerTournament = createTournamentDto.Settings.ParticipantsPerTournament
                ?? DefaultSettings.TournamentSettings["ParticipantsPerTournament"],
            QuestionsCostMax = createTournamentDto.Settings.QuestionsCostMax
                ?? DefaultSettings.TournamentSettings["QuestionsCostMax"],
            QuestionsCostMin = createTournamentDto.Settings.QuestionsCostMin
                ?? DefaultSettings.TournamentSettings["QuestionsCostMin"],
            QuestionsPerTopicMax = createTournamentDto.Settings.QuestionsPerTopicMax
                ?? DefaultSettings.TournamentSettings["QuestionsPerTopicMax"],
            QuestionsPerTopicMin = createTournamentDto.Settings.QuestionsPerTopicMin
                ?? DefaultSettings.TournamentSettings["QuestionsPerTopicMin"],
            TopicsAuthorsMax = createTournamentDto.Settings.TopicsAuthorsMax
                ?? DefaultSettings.TournamentSettings["TopicsAuthorsMax"],
            TopicsPerParticipantMax = createTournamentDto.Settings.TopicsPerParticipantMax
                ?? DefaultSettings.TournamentSettings["TopicsPerParticipantMax"],
            TopicsPerParticipantMin = createTournamentDto.Settings.TopicsPerParticipantMin
                ?? DefaultSettings.TournamentSettings["TopicsPerParticipantMin"],
            TopicsPerMatch = createTournamentDto.Settings.TopicsPerMatch
                ?? DefaultSettings.TournamentSettings["TopicsPerMatch"],
            TournamentId = tournamentDomain.Id
        };

        await _settingsRepository.CreateAsync(settingsDomain);

        Tournament? tournamentIncludeSettings = await _tournamentRepository.GetByIdIncludeSettingsAsync(tournamentDomain.Id);

        if (tournamentIncludeSettings is null)
            return BadRequest();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentIncludeSettings);

        return CreatedAtAction(nameof(GetById), new { id = tournamentDomain.Id }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:int}")]
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
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.DeleteAsync(id);

        if (tournamentDomain is null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }
}
