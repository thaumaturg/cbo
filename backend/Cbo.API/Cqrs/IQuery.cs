namespace Cbo.API.Cqrs;

/// <summary>A query whose handler returns <see cref="Cbo.Results.Result{T}"/>. <typeparamref name="TResult"/> is never stored.</summary>
public interface IQuery<TResult>;
