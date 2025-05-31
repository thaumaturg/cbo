using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Tournament, TournamentDto>().ReverseMap();
        CreateMap<Tournament, AddTournamentRequestDto>().ReverseMap();
        CreateMap<Tournament, UpdateTournamentRequestDto>().ReverseMap();
    }
}
