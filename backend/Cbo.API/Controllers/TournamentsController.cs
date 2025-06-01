using AutoMapper;
using Cbo.API.Data;
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
    private readonly CboDbContext _dbContext;
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;

    public TournamentsController(CboDbContext dbContext, ITournamentRepository tournamentRepository, ISettingsRepository settingsRepository, IMapper mapper)
    {
        _dbContext = dbContext;
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

        if (tournamentDomain == null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto addTournamentRequestDto)
    {
        Tournament tournamentDomain = _mapper.Map<Tournament>(addTournamentRequestDto);

        tournamentDomain = await _tournamentRepository.CreateAsync(tournamentDomain);

        var settingsDomain = new Settings
        {
            ParticipantsPerMatch = addTournamentRequestDto.Settings.ParticipantsPerMatch
                ?? DefaultSettings.TournamentSettings["ParticipantsPerMatch"],
            ParticipantsPerTournament = addTournamentRequestDto.Settings.ParticipantsPerTournament
                ?? DefaultSettings.TournamentSettings["ParticipantsPerTournament"],
            QuestionsCostMax = addTournamentRequestDto.Settings.QuestionsCostMax
                ?? DefaultSettings.TournamentSettings["QuestionsCostMax"],
            QuestionsCostMin = addTournamentRequestDto.Settings.QuestionsCostMin
                ?? DefaultSettings.TournamentSettings["QuestionsCostMin"],
            QuestionsPerTopicMax = addTournamentRequestDto.Settings.QuestionsPerTopicMax
                ?? DefaultSettings.TournamentSettings["QuestionsPerTopicMax"],
            QuestionsPerTopicMin = addTournamentRequestDto.Settings.QuestionsPerTopicMin
                ?? DefaultSettings.TournamentSettings["QuestionsPerTopicMin"],
            TopicsAuthorsMax = addTournamentRequestDto.Settings.TopicsAuthorsMax
                ?? DefaultSettings.TournamentSettings["TopicsAuthorsMax"],
            TopicsPerParticipantMax = addTournamentRequestDto.Settings.TopicsPerParticipantMax
                ?? DefaultSettings.TournamentSettings["TopicsPerParticipantMax"],
            TopicsPerParticipantMin = addTournamentRequestDto.Settings.TopicsPerParticipantMin
                ?? DefaultSettings.TournamentSettings["TopicsPerParticipantMin"],
            TopicsPerMatch = addTournamentRequestDto.Settings.TopicsPerMatch
                ?? DefaultSettings.TournamentSettings["TopicsPerMatch"],
            TournamentId = tournamentDomain.Id
        };

        settingsDomain = await _settingsRepository.CreateAsync(settingsDomain);

        CreateTournamentDto tournamentDto = _mapper.Map<CreateTournamentDto>(tournamentDomain);

        return CreatedAtAction(nameof(GetById), new { id = tournamentDomain.Id }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTournamentDto updateTournamentRequestDto)
    {
        Tournament? tournamentDomain = _mapper.Map<Tournament>(updateTournamentRequestDto);

        tournamentDomain = await _tournamentRepository.UpdateAsync(id, tournamentDomain);

        if (tournamentDomain == null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        Tournament? tournamentDomain = await _tournamentRepository.DeleteAsync(id);

        if (tournamentDomain == null)
            return NotFound();

        GetTournamentDto tournamentDto = _mapper.Map<GetTournamentDto>(tournamentDomain);

        return Ok(tournamentDto);
    }
}
