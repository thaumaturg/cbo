using AutoMapper;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Tournament, GetTournamentDto>().ReverseMap();
        CreateMap<Tournament, CreateTournamentDto>().ReverseMap();
        CreateMap<Tournament, UpdateTournamentDto>().ReverseMap();
        CreateMap<Topic, GetTopicDto>().ReverseMap();
        CreateMap<Topic, CreateTopicDto>().ReverseMap();
        CreateMap<Topic, UpdateTopicDto>().ReverseMap();
        CreateMap<Question, GetQuestionDto>().ReverseMap();
        CreateMap<Question, CreateQuestionDto>().ReverseMap();
        CreateMap<Question, UpdateQuestionDto>().ReverseMap();
        CreateMap<Round, GetRoundDto>().ReverseMap();
        CreateMap<Round, CreateRoundDto>().ReverseMap();
        CreateMap<Round, UpdateRoundDto>().ReverseMap();
        CreateMap<RoundAnswer, GetRoundAnswerDto>().ReverseMap();
        CreateMap<RoundAnswer, CreateRoundAnswerDto>().ReverseMap();
        CreateMap<RoundAnswer, UpdateRoundAnswerDto>().ReverseMap();
        CreateMap<TournamentParticipant, GetTournamentParticipantDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.ApplicationUser.UserName));
        CreateMap<TournamentParticipant, CreateTournamentParticipantDto>().ReverseMap();
        CreateMap<TournamentParticipant, UpdateTournamentParticipantDto>().ReverseMap();
        CreateMap<TopicAuthor, GetTopicAuthorDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.ApplicationUser.UserName));
    }
}
