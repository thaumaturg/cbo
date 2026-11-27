using Cbo.Core.Cqrs;
using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

public sealed record RollBackCommand(Guid TournamentId, TournamentStage Stage) : ICommand;
