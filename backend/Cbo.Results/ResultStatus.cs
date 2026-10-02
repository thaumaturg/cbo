namespace Cbo.Results;

/// <summary>
/// The semantic outcome of an operation. Deliberately not HTTP status codes:
/// the HTTP mapping lives in the API layer.
/// </summary>
public enum ResultStatus
{
    /// <summary>The operation succeeded.</summary>
    Ok,

    /// <summary>The operation created a new resource; see <see cref="Result.Location"/>.</summary>
    Created,

    /// <summary>The operation succeeded and there is nothing to return.</summary>
    NoContent,

    /// <summary>The input failed validation.</summary>
    Invalid,

    /// <summary>The target resource does not exist or is not visible to the caller.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state of the resource.</summary>
    Conflict,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not permitted.</summary>
    Forbidden,

    /// <summary>An infrastructure-level failure that is still a modelled outcome (e.g. a reload after a write returned nothing).</summary>
    Unexpected
}
