# Hand-rolled Result type with semantic failure kinds

Services report expected failures (not found, invalid input, conflicting state, missing permission, bad credentials) as values of our own small `Result` type rather than exceptions; unexpected failures stay exceptions and are turned into a 500 problem response by a global handler. A failure is classified by one of five semantic kinds (Invalid, NotFound, Conflict, Forbidden, Unauthorized), never by an HTTP status code, and the API project translates kinds to HTTP in exactly one place. We wrote the type ourselves because every library would still have required our own HTTP / RFC 9457 mapping, so it would only have supplied the ~200-line type itself, and owning that type is part of this project's learning goal.

## Considered options

- **Ardalis.Result**: closest fit (status enum, field-bound validation errors), but its 11 statuses include exception-like `Error`/`CriticalError`/`Unavailable`, its ProblemDetails mapping joins all messages into one `detail` string and would be replaced anyway, `Result`/`IResult` collide with ASP.NET Core names, and there has been no release since October 2024.
- **ErrorOr**: every error requires a `Code` (we decided not to have error codes yet), field names can only travel inside `Code`, and as a struct `default(ErrorOr<T>)` counts as success.
- **FluentResults**: no notion of failure kind at all, mutable, and its ASP.NET Core extension maps every failure to 400 and depends on an ASP.NET Core 2.x-era package.
- **HTTP status code as the failure kind** (the design of the reference implementation in another project): that design pays off when the same C# `Result` type lives on both sides of the network (server and generated C# client). Here the client is Vue and only sees problem JSON, so it would just make services speak HTTP.

Licensing was not a deciding factor: all candidates are MIT, and a published MIT version cannot be retroactively relicensed.

## Consequences

- Failure kind meanings (kept consistent across the codebase):
  - **Invalid** (400): the request is wrong in itself, whatever the current data looks like. This includes ids in the body that don't exist.
  - **NotFound** (404): the resource in the URL doesn't exist.
  - **Conflict** (409): the request is valid, but the current state doesn't allow it (stage rules, an already played topic, duplicates).
  - **Forbidden** (403): the resource exists, but the user may not perform this operation on it.
  - **Unauthorized** (401): the credentials are wrong.
- Status codes are honest: a failed permission check is 403, not 404. Hiding whether a resource exists (to prevent enumeration) was considered and dropped as unnecessary for this app.
- No new kind is added until a real case needs one.
