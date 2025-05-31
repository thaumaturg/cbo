using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TournamentsController : ControllerBase
{
    private readonly CboDbContext _dbContext;

    public TournamentsController(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tournamentsDomain = await _dbContext.Tournaments.ToListAsync();
        var tournamentsDto = new List<TournamentDto>();

        foreach (var tournament in tournamentsDomain)
        {
            tournamentsDto.Add(new TournamentDto()
            {
                Id = tournament.Id,
                Title = tournament.Title,
                Description = tournament.Description,
                CurrentStage = tournament.CurrentStage.ToString(),
                PlannedStart = tournament.PlannedStart,
                CreatedAt = tournament.CreatedAt,
                StartedAt = tournament.StartedAt,
                EndedAt = tournament.EndedAt,
            });
        }

        return Ok(tournamentsDto);
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var tournamentDomain = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (tournamentDomain == null)
            return NotFound();

        var tournamentDto = new TournamentDto()
        {
            Id = id,
            Title = tournamentDomain.Title,
            Description = tournamentDomain.Description,
            CurrentStage = tournamentDomain.CurrentStage.ToString(),
            PlannedStart = tournamentDomain.PlannedStart,
            CreatedAt = tournamentDomain.CreatedAt,
            StartedAt = tournamentDomain.StartedAt,
            EndedAt = tournamentDomain.EndedAt,
        };

        return Ok(tournamentDto);
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddTournamentRequestDto addTournamentRequestDto)
    {
        var tournamentDomain = new Tournament
        {
            Title = addTournamentRequestDto.Title,
            Description = addTournamentRequestDto.Description,
            CurrentStage = TournamentStage.Preparations,
            PlannedStart = addTournamentRequestDto.PlannedStart,
        };

        await _dbContext.Tournaments.AddAsync(tournamentDomain);
        await _dbContext.SaveChangesAsync();

        var settings = new Settings
        {
            ParticipantsPerMatchMax = addTournamentRequestDto.Settings.ParticipantsPerMatchMax
                ?? DefaultSettings.TournamentSettings["ParticipantsPerMatchMax"],
            ParticipantsPerTournamentMax = addTournamentRequestDto.Settings.ParticipantsPerTournamentMax
                ?? DefaultSettings.TournamentSettings["ParticipantsPerTournamentMax"],
            ParticipantsPerTournamentMin = addTournamentRequestDto.Settings.ParticipantsPerTournamentMin
                ?? DefaultSettings.TournamentSettings["ParticipantsPerTournamentMin"],
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

        await _dbContext.Settings.AddAsync(settings);
        await _dbContext.SaveChangesAsync();

        var tournamentDto = new TournamentDto()
        {
            Id = tournamentDomain.Id,
            Title = tournamentDomain.Title,
            Description = tournamentDomain.Description,
            CurrentStage = tournamentDomain.CurrentStage.ToString(),
            PlannedStart = tournamentDomain.PlannedStart,
            CreatedAt = tournamentDomain.CreatedAt,
            StartedAt = tournamentDomain.StartedAt,
            EndedAt = tournamentDomain.EndedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = tournamentDomain.Id }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTournamentRequestDto updateTournamentRequestDto)
    {
        var tournamentDomain = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (tournamentDomain == null)
            return NotFound();

        tournamentDomain.Title = updateTournamentRequestDto.Title;
        tournamentDomain.Description = updateTournamentRequestDto.Description;
        tournamentDomain.PlannedStart = updateTournamentRequestDto.PlannedStart;

        await _dbContext.SaveChangesAsync();

        var tournamentDto = new TournamentDto()
        {
            Id = tournamentDomain.Id,
            Title = tournamentDomain.Title,
            Description = tournamentDomain.Description,
            CurrentStage = tournamentDomain.CurrentStage.ToString(),
            PlannedStart = tournamentDomain.PlannedStart,
            CreatedAt = tournamentDomain.CreatedAt,
            StartedAt = tournamentDomain.StartedAt,
            EndedAt = tournamentDomain.EndedAt
        };

        return Ok(tournamentDto);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var tournamentDomain = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (tournamentDomain == null)
            return NotFound();

        _dbContext.Tournaments.Remove(tournamentDomain);
        await _dbContext.SaveChangesAsync();

        var tournamentDto = new TournamentDto()
        {
            Id = tournamentDomain.Id,
            Title = tournamentDomain.Title,
            Description = tournamentDomain.Description,
            CurrentStage = tournamentDomain.CurrentStage.ToString(),
            PlannedStart = tournamentDomain.PlannedStart,
            CreatedAt = tournamentDomain.CreatedAt,
            StartedAt = tournamentDomain.StartedAt,
            EndedAt = tournamentDomain.EndedAt
        };

        return Ok(tournamentDto);
    }
}
