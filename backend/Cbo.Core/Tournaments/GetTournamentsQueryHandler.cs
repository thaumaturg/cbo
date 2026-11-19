using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Results;

namespace Cbo.Core.Tournaments;

public sealed class GetTournamentsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentsQuery, PageResponse<TournamentResponse>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<PageResponse<TournamentResponse>>> HandleAsync(GetTournamentsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        List<FieldError> errors = Validate(query);
        if (errors.Count > 0)
            return Failure.Invalid(PagingRules.InvalidMessage, errors);

        return await TournamentListFilter.Apply(_dbContext.Tournaments, query, currentUserId)
            .ToPageAsync(
                TournamentListFilter.SortKeys,
                query.Sort,
                query.Page,
                query.PageSize,
                TournamentProjections.ToResponse(currentUserId),
                cancellationToken);
    }

    private static List<FieldError> Validate(GetTournamentsQuery query)
    {
        List<FieldError> errors = PagingRules.Validate(query.Page, query.PageSize, query.Sort, TournamentListFilter.SortKeys);

        // Binding accepts numbers for enums, so an undefined value can still get here
        if (query.Stage is { } stage && !Enum.IsDefined(stage))
            errors.Add(new FieldError("stage", UnknownValueMessage<TournamentStageFilter>("stage")));

        if (query.Roles.Any(role => !Enum.IsDefined(role)))
            errors.Add(new FieldError("role", UnknownValueMessage<TournamentParticipantRole>("role")));

        return errors;
    }

    private static string UnknownValueMessage<TEnum>(string name) where TEnum : struct, Enum =>
        $"Unknown {name}. Allowed: {string.Join(", ", Enum.GetNames<TEnum>())}.";
}
