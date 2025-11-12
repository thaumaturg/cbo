namespace Cbo.API.Services;

using System;
using AutoMapper;
using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Identity;

public class TournamentService
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentParticipantsRepository _tournamentParticipantsRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CboDbContext _dbContext;
    private readonly IMapper _mapper;

    public TournamentService(
        ITournamentRepository tournamentRepository,
        ITournamentParticipantsRepository tournamentParticipantsRepository,
        UserManager<ApplicationUser> userManager,
        CboDbContext dbContext,
        IMapper mapper)
    {
        _tournamentRepository = tournamentRepository;
        _tournamentParticipantsRepository = tournamentParticipantsRepository;
        _userManager = userManager;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<int> createTournamentWithParticipants(CreateTournamentDto createTournamentDto)
    {
        Tournament tournament = _mapper.Map<Tournament>(createTournamentDto);

        foreach (string userName in createTournamentDto.UserNames)
        {
            ApplicationUser? user = _userManager.FindByNameAsync(userName).Result;

            if (user != null)
            {
                TournamentParticipant participant = new TournamentParticipant
                {
                    Role = Models.Constants.TournamentParticipantRole.Player,
                    PointsSum = 0,
                    ApplicationUserId = user.Id,
                };

                tournament.TournamentParticipants.Add(participant);
            }
        }

        await _dbContext.Tournaments.AddAsync(tournament);
        await _dbContext.SaveChangesAsync();

        return tournament.Id;
    }

    public void AdvanceTournamentToStage(int id, int stage)
    {
        Tournament tournament = _dbContext.Tournaments.Find(id);
        TournamentStage newStage = (TournamentStage)stage;

        if (newStage != TournamentStage.Qualifications)
        {
            throw new NotImplementedException("Only update to qualifications is implemented");
        }

       // implement
    }
}