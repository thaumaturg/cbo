using Cbo.Core.Domain;
using FluentValidation;

namespace Cbo.Core.Participants;

/// <summary>
/// Rules for the fields that the participant request validators share.
/// </summary>
internal static class ParticipantRequestRules
{
    /// <summary>
    /// A defined role other than <see cref="TournamentParticipantRole.Creator"/>: a tournament has exactly one creator,
    /// set when it's created. <paramref name="creatorMessage"/> explains the creator error for the request at hand.
    /// </summary>
    public static IRuleBuilderOptions<T, TournamentParticipantRole> AssignableRole<T>(
        this IRuleBuilder<T, TournamentParticipantRole> rule, string creatorMessage) =>
        rule
            .IsInEnum()
            .WithMessage($"Unknown role. Allowed: {string.Join(", ", Enum.GetNames<TournamentParticipantRole>())}.")
            .NotEqual(TournamentParticipantRole.Creator)
            .WithMessage(creatorMessage);
}
