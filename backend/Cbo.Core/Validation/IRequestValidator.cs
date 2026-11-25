namespace Cbo.Core.Validation;

/// <summary>
/// The non-generic view of a <see cref="RequestValidator{T}"/>, so the endpoint filter can read the message for a
/// validator it found by argument type.
/// </summary>
public interface IRequestValidator
{
    /// <summary>
    /// The problem <c>detail</c> when the request is invalid; the field errors say what is wrong.
    /// </summary>
    string InvalidMessage { get; }
}
