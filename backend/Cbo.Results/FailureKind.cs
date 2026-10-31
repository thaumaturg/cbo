namespace Cbo.Results;

/// <summary>
/// The semantic kind of an expected failure. See docs/adr/0002-hand-rolled-result-with-failure-kinds.md.
/// </summary>
public enum FailureKind
{
    /// <summary>The request is wrong in itself, whatever the current data. This includes ids in the body that don't exist.</summary>
    Invalid,

    /// <summary>The resource identified by the URL doesn't exist.</summary>
    NotFound,

    /// <summary>The request is valid, but the current state doesn't allow it.</summary>
    Conflict,

    /// <summary>The resource exists, but the user may not perform this operation on it.</summary>
    Forbidden,

    /// <summary>The credentials are wrong.</summary>
    Unauthorized,
}
