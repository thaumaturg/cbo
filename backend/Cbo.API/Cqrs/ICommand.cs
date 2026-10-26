namespace Cbo.API.Cqrs;

/// <summary>A command whose handler returns <see cref="Cbo.Results.Result"/>.</summary>
public interface ICommand;

/// <summary>A command whose handler returns <see cref="Cbo.Results.Result{T}"/>. <typeparamref name="TResult"/> is never stored.</summary>
public interface ICommand<TResult>;
