using Cbo.API.Data;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cbo.API.Cqrs;

/// <summary>
/// Runs a command in one transaction with one save: <see cref="ExecuteAsync"/> stages changes, then the base class saves
/// and commits. A failure or an exception rolls everything back. The only place that saves or opens a transaction.
/// </summary>
public abstract class CommandHandler<TCommand>(CboDbContext dbContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected CboDbContext DbContext { get; } = dbContext;

    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        // Disposing a transaction that wasn't committed rolls it back, including when ExecuteAsync throws
        await using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        Result result = await ExecuteAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            DbContext.ChangeTracker.Clear();
            return result;
        }

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            Failure? failure = MapSaveFailure(command, exception);
            if (failure is null)
                throw;

            DbContext.ChangeTracker.Clear();
            return failure;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Runs the use case: checks in whatever order the use case needs, then stages changes on <see cref="DbContext"/>.
    /// Never calls SaveChangesAsync.
    /// </summary>
    protected abstract Task<Result> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Translates an expected constraint violation (a lost race) into a failure. Null rethrows, which gives a 500.
    /// </summary>
    protected virtual Failure? MapSaveFailure(TCommand command, DbUpdateException exception) => null;
}

/// <inheritdoc cref="CommandHandler{TCommand}"/>
public abstract class CommandHandler<TCommand, TResult>(CboDbContext dbContext) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    protected CboDbContext DbContext { get; } = dbContext;

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        // Disposing a transaction that wasn't committed rolls it back, including when ExecuteAsync throws
        await using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        Result<TResult> result = await ExecuteAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            DbContext.ChangeTracker.Clear();
            return result;
        }

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            Failure? failure = MapSaveFailure(command, exception);
            if (failure is null)
                throw;

            DbContext.ChangeTracker.Clear();
            return failure;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc cref="CommandHandler{TCommand}.ExecuteAsync"/>
    protected abstract Task<Result<TResult>> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <inheritdoc cref="CommandHandler{TCommand}.MapSaveFailure"/>
    protected virtual Failure? MapSaveFailure(TCommand command, DbUpdateException exception) => null;
}
