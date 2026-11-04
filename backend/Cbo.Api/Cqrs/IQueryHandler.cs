using Cbo.Results;

namespace Cbo.Api.Cqrs;

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
