using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Tournament, GetTournamentDto>().ReverseMap();
        CreateMap<Tournament, AddTournamentRequestDto>().ReverseMap();
        CreateMap<Tournament, UpdateTournamentRequestDto>().ReverseMap();
        CreateMap<Settings, SettingsCreateDto>().ReverseMap();
    }
}
