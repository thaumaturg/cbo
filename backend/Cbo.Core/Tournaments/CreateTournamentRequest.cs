using System.Text.Json.Serialization;
using Cbo.Core.Domain;
using Cbo.Core.Validation;

namespace Cbo.Core.Tournaments;

public record CreateTournamentRequest
{
    public required string Title { get; set => field = RequestText.Trim(value); }
    public string? Description { get; set => field = RequestText.TrimToNull(value); }

    [JsonConverter(typeof(UnknownEnumNameConverter<TournamentFormat>))]
    public TournamentFormat? Format { get; set; }

    public int? TopicsPerParticipantMax { get; set; }
    public int? TopicsPerParticipantMin { get; set; }
}
