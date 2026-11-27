using System.Collections.Immutable;
using Cbo.Core.Domain;

namespace Cbo.Core.Playoffs;

/// <summary>Where settling a playoff match or the Final stands after the scores played so far.</summary>
public abstract record Settlement<TPlayer>
    where TPlayer : notnull;

/// <summary>
/// The match isn't decided yet: <see cref="Players"/>, still tied, play the next shootout for <see cref="Places"/>.
/// In a playoff match those are the winner places still open, and the rest of the group lose; in the Final, every
/// place the group covers.
/// </summary>
public sealed record ShootoutNeeded<TPlayer>(ImmutableArray<TPlayer> Players, ImmutableArray<Outcome> Places)
    : Settlement<TPlayer>
    where TPlayer : notnull;

/// <summary>The match is Decided: every player has an <see cref="Outcome"/>.</summary>
public sealed record Decided<TPlayer>(ImmutableDictionary<TPlayer, Outcome> Outcomes) : Settlement<TPlayer>
    where TPlayer : notnull;
