using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Tournaments;

public sealed class GetTournamentsRequestValidator : RequestValidator<GetTournamentsRequest>
{
    public GetTournamentsRequestValidator()
    {
        RuleFor(r => r.Page).Page();
        RuleFor(r => r.PageSize).PageSize();
        RuleFor(r => r.Sort).Sort(TournamentListFilter.SortKeys);
        RuleFor(r => r.Search).Search();

        // Binding accepts numbers for enums, so an undefined value can still get here
        RuleFor(r => r.Stage)
            .IsInEnum()
            .WithMessage(UnknownValueMessage<TournamentStageFilter>("stage"));
        RuleFor(r => r.Role)
            .Must(roles => roles is null || roles.All(Enum.IsDefined))
            .WithMessage(UnknownValueMessage<TournamentParticipantRole>("role"));
    }

    public override string InvalidMessage => PagingRules.InvalidMessage;

    private static string UnknownValueMessage<TEnum>(string name) where TEnum : struct, Enum =>
        $"Unknown {name}. Allowed: {string.Join(", ", Enum.GetNames<TEnum>())}.";
}
