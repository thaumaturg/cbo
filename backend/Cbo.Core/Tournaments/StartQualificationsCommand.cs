using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record StartQualificationsCommand(Guid TournamentId) : ICommand;
