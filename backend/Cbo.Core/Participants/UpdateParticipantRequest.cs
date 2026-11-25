using System.Text.Json.Serialization;
using Cbo.Core.Domain;
using Cbo.Core.Validation;

namespace Cbo.Core.Participants;

public record UpdateParticipantRequest
{
    [JsonConverter(typeof(UnknownEnumNameConverter<TournamentParticipantRole>))]
    public required TournamentParticipantRole Role { get; set; }
}
