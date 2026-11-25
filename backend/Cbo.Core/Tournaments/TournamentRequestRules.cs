using Cbo.Core.Domain;
using FluentValidation;

namespace Cbo.Core.Tournaments;

/// <summary>
/// Rules for the fields that the create and update tournament request validators share. A setting the Creator leaves
/// out is checked with its default filled in, the way <see cref="TournamentSettings.Resolve"/> fills it in.
/// </summary>
internal static class TournamentRequestRules
{
    public static IRuleBuilderOptions<T, string> TournamentTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("The title is required.")
            .Length(Limits.Tournaments.TitleLengthMin, Limits.Tournaments.TitleLengthMax)
            .WithMessage(
                $"The title must be between {Limits.Tournaments.TitleLengthMin} and {Limits.Tournaments.TitleLengthMax} characters.");

    public static IRuleBuilderOptions<T, string?> TournamentDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .MaximumLength(Limits.Tournaments.DescriptionLengthMax)
            .WithMessage($"The description can be at most {Limits.Tournaments.DescriptionLengthMax} characters.");

    public static IRuleBuilderOptions<T, int?> PlayersPerTournament<T>(this IRuleBuilder<T, int?> rule) =>
        rule
            .Must(size => Limits.Tournaments.SupportedSizes.Contains(size ?? TournamentDefaults.PlayersPerTournament))
            .WithMessage($"Tournament size must be one of: {string.Join(", ", Limits.Tournaments.SupportedSizes)}.");

    /// <summary>
    /// The minimum is only a recommendation (ADR 0007), so it has no ceiling of its own. The maximum bounds it.
    /// </summary>
    public static IRuleBuilderOptions<T, int?> TopicsPerParticipantMin<T>(this IRuleBuilder<T, int?> rule) =>
        rule
            .Must(min => ResolveMin(min) >= Limits.Tournaments.TopicsPerParticipantFloor)
            .WithMessage("The minimum number of topics can't be negative.");

    public static IRuleBuilderOptions<T, int?> TopicsPerParticipantMax<T>(
        this IRuleBuilder<T, int?> rule, Func<T, int?> topicsPerParticipantMin) =>
        rule
            .Must(max => ResolveMax(max) >= Limits.Tournaments.TopicsPerParticipantFloor)
            .WithMessage("The maximum number of topics can't be negative.")
            .Must(max => ResolveMax(max) <= Limits.Tournaments.TopicsPerParticipantCeiling)
            .WithMessage(
                $"The maximum number of topics can be at most {Limits.Tournaments.TopicsPerParticipantCeiling}.")
            .Must((request, max) => ResolveMax(max) >= ResolveMin(topicsPerParticipantMin(request)))
            .WithMessage((request, _) =>
                $"The maximum number of topics can't be less than the minimum ({ResolveMin(topicsPerParticipantMin(request))}).");

    private static int ResolveMin(int? min) => min ?? TournamentDefaults.TopicsPerParticipantMin;

    private static int ResolveMax(int? max) => max ?? TournamentDefaults.TopicsPerParticipantMax;
}
