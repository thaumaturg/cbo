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
        CreateMap<Round, GetRoundDto>()
            .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src => src.Topic.Title))
            .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Topic.Questions.OrderBy(q => q.QuestionNumber)))
            .ForMember(dest => dest.TopicPriorityIndex, opt => opt.MapFrom(src =>
                src.Match.Tournament.TournamentTopics.First(tt => tt.TopicId == src.TopicId).PriorityIndex))
            .ForMember(dest => dest.TopicOwnerUsername, opt => opt.MapFrom(src =>
                src.Match.Tournament.TournamentTopics.First(tt => tt.TopicId == src.TopicId).TournamentParticipant.ApplicationUser.UserName));
        CreateMap<RoundAnswer, GetRoundAnswerDto>().ReverseMap();
        CreateMap<RoundAnswer, CreateRoundAnswerDto>().ReverseMap();
        CreateMap<CreateRoundWithAnswersDto, Round>()
            .ForMember(dest => dest.RoundAnswers, opt => opt.MapFrom(src => src.Answers));
        CreateMap<TournamentParticipant, GetTournamentParticipantDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.ApplicationUser.UserName))
            .ForMember(dest => dest.TopicsCount, opt => opt.MapFrom(src => src.TournamentTopics.Count));
        CreateMap<TournamentParticipant, CreateTournamentParticipantDto>().ReverseMap();
        CreateMap<TournamentParticipant, UpdateTournamentParticipantDto>().ReverseMap();
        CreateMap<TopicAuthor, GetTopicAuthorDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.ApplicationUser.UserName));
        CreateMap<TournamentTopic, GetTournamentTopicDto>()
            .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src => src.Topic.Title))
            .ForMember(dest => dest.ParticipantUsername, opt => opt.MapFrom(src => src.TournamentParticipant.ApplicationUser.UserName));
        CreateMap<UpdateTournamentTopicDto, TournamentTopic>();
        CreateMap<Match, GetMatchDto>()
            .ForMember(dest => dest.RoundsCount, opt => opt.MapFrom(src => src.Rounds.Count));
        CreateMap<MatchParticipant, GetMatchParticipantDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.TournamentParticipant.ApplicationUser.UserName));
        CreateMap<Match, GetMatchDetailDto>();
        CreateMap<TournamentTopic, GetAvailableTopicDto>()
            .ForMember(dest => dest.OwnerUsername, opt => opt.MapFrom(src => src.TournamentParticipant.ApplicationUser.UserName))
            .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src => src.Topic.Title));
    }
}
