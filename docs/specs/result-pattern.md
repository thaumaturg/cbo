# Spec: Consistent API errors and a Result type for expected failures

Status: ready for implementation
Decision record: ADR 0001 (hand-rolled Result with failure kinds)
Origin: `/grill-with-docs` session, questions Q1–Q31. The index at the end maps every question to where it is used in this spec.

## Problem Statement

Players, organizers and tournament creators get confusing error feedback from the app. Sometimes a toast shows `[object Object]` instead of a message. The same mistake gets a different status code depending on the endpoint ("topic already played" is 400 in one place and 409 in another). When a user lacks permission, the app says the tournament "doesn't exist", even if they are looking at it.

For the developer, the backend has no single way to say "this operation failed, and here is why":

- Business rules live directly in about 34 controller actions.
- Failures are signalled in five different ways: nullable returns, an error string, an error dictionary, built-in exceptions (some never caught), and silently doing nothing.
- Error responses are sometimes a bare string, sometimes RFC 9457 problem details, and sometimes validation problem details.
- There is no global exception handler, so an unexpected exception produces the developer page in Development and an empty 500 in Production.
- The Vue frontend copies the same error-handling block about 33 times and can't rely on any one error shape.

## Solution

Two changes:

1. **Every non-2xx response from the API is an RFC 9457 problem details document.** It is a validation problem details document when field errors are present. Unexpected exceptions become a logged 500 problem response with a trace id. The frontend turns any API error into one readable message through a single helper.
2. **A small hand-rolled `Result` type for expected failures.** It lives in its own library with no HTTP dependency. Each failure has one of five kinds (Invalid, NotFound, Conflict, Forbidden, Unauthorized), a required message, and optional field errors. Services return Results, and the API translates a failure kind to an HTTP response in exactly one place. Features move to this model one at a time, starting with Rounds.

Status codes are honest: a missing permission is 403 Forbidden, never a disguised 404.

The work ships as three slices, in this order:

1. Error JSON everywhere, plus the frontend error helper. This changes the response format only.
2. The Result library and its test project, with no callers yet.
3. Migrating Rounds to services that return Results, plus three bug fixes.

## User Stories

### End users (players, organizers, creators)

1. As a player, I want every error toast to show a readable sentence, so that I never see `[object Object]` or raw JSON.
2. As a creator, I want to be told "you don't have permission" when I can see a tournament but lack the role for an action, so that I'm not told a tournament I'm looking at doesn't exist.
3. As a creator recording a round, I want a clear message when the round number is out of range, so that I can fix my input.
4. As a creator recording a round, I want a clear message when the round already exists, so that I know to edit it instead of creating it.
5. As a creator recording a round, I want to be stopped from using a topic that isn't part of this tournament, so that I don't get a server error and an inconsistent state.
6. As a creator recording a round, I want to see which answers are wrong when the answers don't fit the chosen scoring mode, so that I can correct them.
7. As a creator, I want two simultaneous attempts to create the same round to end with one round and one clear conflict message, so that the match never has duplicate rounds.
8. As a creator, I want a clear message when I try to change a round's topic, number or mode, so that I understand I must delete and recreate it.
9. As a user, I want validation failures to list each problem rather than a generic "bad request", so that I can fix everything in one go.
10. As a user, I want an unexpected server failure to show a generic message, while the server logs the details, so that I'm not shown internals and the problem can still be traced.
11. As a user whose session expired, I want the existing "please log in again" behaviour to keep working, so that the error changes don't break authentication.

### Frontend developer (Vue)

12. As a frontend developer, I want every API error to have one shape (problem details), so that I can handle errors in one place.
13. As a frontend developer, I want one helper that turns any API error into a message, so that I can delete the copy-pasted handling in every service.
14. As a frontend developer, I want the helper to prefer the specific `detail`, then field-error messages, then the generic `title`, then my fallback text, so that the most useful text is shown.
15. As a frontend developer, I want every service method to return the same failure envelope with a string message, so that components can render `error` without checking its type.
16. As a frontend developer, I want the authentication service and store to use the same helper and envelope, so that login, register and change-password behave like everything else.
17. As a frontend developer, I want field-error keys to be camelCase JSON paths that match the property names I send, so that I can later show errors next to form fields without translating names.
18. As a frontend developer, I want the automatic model-validation 400 to use the same camelCase keys, so that there is only one naming scheme to handle.
19. As a frontend developer, I want status codes that mean the same thing on every endpoint, so that I can rely on 403, 404 and 409 when I need to.

### Backend developer

20. As a backend developer, I want one `Failure` value (kind, message, optional field errors) to describe any expected failure, so that I stop choosing between nulls, strings, dictionaries and exceptions.
21. As a backend developer, I want five failure kinds with written-down meanings, so that the same situation always gets the same kind.
22. As a backend developer, I want services to know nothing about HTTP, so that business rules read as business rules.
23. As a backend developer, I want to write `return Failure.NotFound("…")` or `return value;` from a method returning a Result, so that returning outcomes is short.
24. As a backend developer, I want to pass a failure on with `return result.Failure;` whatever the target result type is, so that I don't need conversion helpers.
25. As a backend developer, I want the compiler to warn me when I read a value or failure without checking which one I have, so that I don't introduce null bugs.
26. As a backend developer, I want reading the wrong side of a result to throw immediately, so that misuse fails loudly instead of producing defaults.
27. As a backend developer, I want a success to be unable to carry errors, so that a 200 response with errors attached is impossible by construction.
28. As a backend developer, I want a failure without a message to be rejected, so that every failure the user sees has text.
29. As a backend developer, I want an explicit success factory alongside the implicit conversions, so that I can return results whose value type is an interface.
30. As a backend developer, I want exactly one place that maps a failure kind to an HTTP problem response, so that changing a mapping is a one-line change.
31. As a backend developer, I want controllers to decide the success response (200, 201 with Location, 204) themselves, so that the HTTP shape of success stays an HTTP decision.
32. As a backend developer, I want services to load the resource, check permission and act, so that controllers only check that the user is logged in.
33. As a backend developer, I want repositories to keep returning nullable values and lists, so that "not found" is turned into NotFound or Invalid only where the reason is known.
34. As a backend developer, I want Postgres unique and foreign-key violations turned into Conflict inside the service, so that races and stale references give a proper 409 rather than a 500.
35. As a backend developer, I want "impossible" states, such as a row I just wrote going missing, to throw, so that bugs reach the global handler as 500s instead of pretending the caller can fix them.
36. As a backend developer, I want unexpected exceptions logged once by a global handler with the trace id, so that I can correlate a user report with the log.
37. As a backend developer, I want expected failures not to be logged, so that the logs aren't flooded with normal 404s and 409s.
38. As a backend developer, I want exception details in error responses only in Development, so that production never leaks internals.
39. As a backend developer, I want the Result library to have no ASP.NET Core reference, so that HTTP concerns can't leak into it.
40. As a backend developer, I want the old string-body responses converted without changing their status codes in slice 1, so that the first slice is mechanical and easy to review.
41. As a backend developer, I want status codes corrected only as each feature moves to Results, so that every behaviour change is reviewed together with the code that justifies it.
42. As a backend developer, I want Rounds to be the first feature migrated, so that the pattern is proven on a feature that already has a service.
43. As a backend developer, I want a GET endpoint for a single round, so that the Location header of a created round points to something real.
44. As a backend developer, I want chaining helpers (Map, Bind, Match) added only once I see the same code repeating, so that the library stays small and every member is justified.

### API consumer (Scalar / OpenAPI user)

45. As an API consumer, I want 401/403/404 responses produced by the framework (for example the JWT challenge) to carry a problem details body too, so that no error response is empty.
46. As an API consumer, I want each problem response to include a `traceId`, so that I can report a failing request precisely.

### Maintainer / reviewer

47. As a maintainer, I want unit tests that pin down the Result type's guarantees, so that the foundation can't regress silently.
48. As a maintainer, I want a single test project that later suites can join, so that the repo doesn't fill up with tiny test projects.
49. As a maintainer, I want the reasons for writing the type ourselves (rather than using Ardalis.Result, ErrorOr or FluentResults) recorded, so that nobody re-opens the question without new information.

## Implementation Decisions

### Goals and scope

- This is a portfolio and learning project. The other project's Result implementation is a **reference, not a template** (Q1). The earlier `result-pattern` branch and its documents are reference only, not authoritative (Q5).
- **Expected failures** (not found, invalid input, conflicting state, missing permission, bad credentials) are Results. **Unexpected failures** (infrastructure outages, bugs) stay exceptions and go to the global handler (Q2).
- Business rules move gradually out of controllers into services that return Results. Rounds is first (Q3).
- **We write the type ourselves; no library** (Q12, ADR 0001). Every library would still need our own HTTP mapping. Licensing was not a factor.

### Failure model (lives in the Result library)

- A failure is classified by a **semantic kind, never by an HTTP status code** (Q6).
- There are exactly five kinds. No new kind is added until a real case needs one (Q13, Q14):

  | Kind | Meaning | HTTP |
  |---|---|---|
  | Invalid | The request is wrong in itself, whatever the current data. This includes ids in the body that don't exist | 400 |
  | NotFound | The resource identified by the URL doesn't exist | 404 |
  | Conflict | The request is valid, but the current state doesn't allow it (stage rules, an already played topic, duplicates) | 409 |
  | Forbidden | The resource exists, but the user may not perform this operation on it | 403 |
  | Unauthorized | The credentials are wrong | 401 |

- **Status codes are honest.** A failed permission check is Forbidden (403), never NotFound. Hiding whether a resource exists was considered and dropped (Q7 reversed by Q14).
- A **`Failure`** consists of a **kind**, a **required, non-empty message** written for the end user, and **zero or more field errors** (Q16, refining Q8).
- A **field error** consists of a field identifier and a message. The field identifier is the **camelCase JSON path as it appears on the wire**, for example `numberInMatch`, `topicId`, `questions[0].text` (Q8, Q29).
- `Failure` exposes one factory per kind. Only Invalid is expected to carry field errors in practice, but no kind forbids them.
- There are **no machine-readable error codes** for now. If one is needed later, problem details' `type` is where it goes (Q9).

### Result type (lives in the Result library)

- Two **immutable sealed classes**: `Result`, for operations without a value, and `Result<T>`. They are classes, not structs, so that `default` can never pass for a valid result (Q17).
- A result is **either a success or holds a `Failure`, by construction**. A success can never carry errors, so the reference design's "self-correcting status" isn't needed (Q17).
- **Implicit conversions:** both types convert from `Failure`, and `Result<T>` also converts from `T`. A failure is passed on with `return result.Failure;`. There is no `As<T>()` (Q17).
- **Explicit factories also exist:** `Result.Success()` and a success factory taking a value. C# ignores user-defined conversions when `T` is an interface type, so `return list;` can't compile for an interface-typed result.
- `IsSuccess` and `IsFailure` are annotated so the compiler knows when `Value` or `Failure` is non-null. **Reading `Value` on a failure, or `Failure` on a success, throws** `InvalidOperationException` (Q17).
- A successful `Result<T>` holds a non-null value. This follows from the non-null guarantee above.
- **No chaining helpers** (Map, Bind, Match, OnSuccess, task extensions) for now. Services use early-return guard clauses. Add a helper only when the Rounds migration shows the same code repeating (Q18).
- The library is a **separate class library (`Cbo.Results`) with no ASP.NET Core reference**. The project boundary enforces that it knows nothing about HTTP (Q19).

### Layering

- **Services** return `Result` or `Result<T>`. Each service method loads the resource, **checks permission through the existing resource-based authorization handlers**, then acts (Q15).
- **Controllers** keep only the coarse `[Authorize]` attribute. They call the service and translate the result: on failure, the single translation method; on success, the controller chooses 200, 201 with Location, or 204 itself (Q15, Q22).
- **Repositories** keep returning nullable values and lists, and never Results. Only the service knows whether a missing row is NotFound (URL) or Invalid (body) (Q23).
- **Database constraint violations:** services catch only the Postgres unique-violation and foreign-key-violation errors and return Conflict. Any other database update exception goes on to the global handler (Q24).
- **Impossible states throw** `InvalidOperationException` and become a 500 through the global handler. They are never Results (Q25).
- **Expected failures are not logged.** Only unexpected exceptions are logged, by the global handler (Q30).

### HTTP boundary (in the API project)

- **One translation point:** an extension method on `ControllerBase`, `Problem(Failure)` (Q6, Q22). It:
  - maps the kind to a status code using the table above
  - builds the body through ASP.NET Core's `ProblemDetailsFactory`, so `type`, `title` and `traceId` are filled in consistently
  - sets `detail` to the failure's message
  - returns a validation problem details document, with `errors` grouped by field identifier, when field errors are present
- No lambda-based or `Match`-style API at the boundary (Q22).
- **Global error handling** uses the framework's built-in pieces (Q20):
  - problem details services are registered
  - an exception-handler middleware with **one** `IExceptionHandler` logs the exception with the trace id and returns a 500 problem response; exception details appear only in Development
  - status-code pages middleware gives empty error responses (for example the JWT challenge 401, or a bare `NotFound()`) a problem details body
- The automatic 400 from `[ApiController]` model validation stays as it is, except that its keys become camelCase JSON names. To do that, the System.Text.Json validation metadata provider is registered with MVC (Q20, Q29).
- **Every non-2xx response is problem details** (Q4).

### Frontend

- One error helper in the existing error utility turns any API error into a single string (Q11, Q28). It uses, in order:
  - `detail` if present
  - **followed by** every field-error message, flattened
  - if there are neither, `title`
  - if there is nothing at all, the caller's fallback text
- Every service method uses the helper and returns the same failure envelope, with `success: false` and `error` as a string. This replaces the roughly 33 copy-pasted blocks, including the special case in change-password and the auth store (Q11).
- Components that render `error` directly keep working, because it is now always a string.
- The existing 401 interceptor (logout and redirect) is unchanged.
- Showing errors next to individual form fields is **deferred**. Field errors are only flattened into the message (Q8).

### Slice 1: error JSON everywhere (format only)

- Register problem details, the global exception handler and status-code pages, as described above (Q20).
- Register the camelCase validation metadata provider (Q29).
- Convert every controller failure response that sends a bare string (fixed strings, interpolated strings, forwarded service strings, exception messages) to a problem response, with `detail` set to that string and **the same status code as today** (Q21).
- Existing validation-problem and `Problem(...)` responses stay.
- Introduce the frontend helper and unified envelope across all services and the auth store (Q11, Q28).
- No status-code corrections and no 404 → 403 changes in this slice (Q21).

### Slice 2: Result library and tests (no callers)

- Create the Result library: failure kind, field error, `Failure`, `Result`, `Result<T>`, as described above (Q16–Q19).
- Create the test project with a Results folder, and add both projects to the solution (Q10, Q19).

### Slice 3: Rounds migration

- The round service gains create, update, delete and get-one operations returning Results. It owns the load → check permission → act sequence and the database transaction that currently sits in the controller (Q3, Q15). The existing answer validation becomes an Invalid failure.
- The Rounds controller actions become thin: call the service, translate failures through the single method, choose the success response (Q22).
- Failure mapping (Q26):

  | Check | Today | Becomes |
  |---|---|---|
  | Tournament or match in the URL missing, or match belongs to another tournament | 404 | NotFound |
  | User lacks the manage-rounds permission | 404 | Forbidden |
  | `numberInMatch` out of range; URL round number ≠ body round number | 400 | Invalid, field `numberInMatch` |
  | Topic id in the body doesn't exist | 400 | Invalid, field `topicId` |
  | Topic exists but isn't part of this tournament (new check) | 500 after commit | Invalid, field `topicId` |
  | Round already exists (create), including a unique-index violation from a race | 400 | Conflict |
  | Round in the URL missing (update, delete, get) | 404 | NotFound |
  | Changing topic, number or mode of an existing round | 400 | Conflict |
  | Answers don't fit the scoring mode | 400 (string) | Invalid, field `answers` |
  | Round just written can't be reloaded; its tournament topic can't be found | 500 `Problem(...)` | Throws, becomes 500 via the global handler (Q25) |

- **Fix 1:** a round's topic must belong to the tournament, checked before anything is written (Q26).
- **Fix 2:** add a unique index on (match, round number) with a migration. The service turns the resulting unique violation into Conflict (Q24, Q26).
- **Fix 3:** add a GET endpoint for a single round by round number, returning the same round representation as create and update. Create returns 201 with a Location header pointing to it (Q26).

## Testing Decisions

- **One test seam: the public API of the Result library**, tested with xUnit in the single test project's Results folder (Q10, Q19, confirmed during /to-spec). Tests only observe public behaviour: construction, properties, conversions and exceptions. They never inspect private state.
- Behaviours to cover:
  - a success has no failure, and reading its `Failure` throws
  - a failure has no value, and reading its `Value` throws
  - `IsSuccess` and `IsFailure` are mutually exclusive
  - an implicit conversion from `Failure` produces a failure of the same kind, message and field errors, for both `Result` and `Result<T>`
  - an implicit conversion from a value produces a success holding that value
  - passing a failure from one `Result<T1>` into a `Result<T2>` keeps its kind, message and field errors
  - each per-kind factory produces its kind
  - an empty or whitespace message is rejected, and so is a field error with an empty field or message
  - field errors given to a failure can't be changed afterwards through the failure
  - the explicit success factories behave like the implicit conversions
- **No automated tests** for slice 1 or slice 3. HTTP-level tests need a web application factory plus Postgres, which is deferred (Q10). Instead, verify by hand in Scalar and the UI:
  - every error is problem details with `traceId`
  - the automatic 400 uses camelCase keys
  - a thrown exception gives a logged 500 with no details outside Development
  - an empty 401 or 404 gets a body
  - no toast shows `[object Object]`
  - every row of the Rounds table returns its new status
  - a 201's Location header resolves through the new GET endpoint
- **Prior art:** none. The repository has no tests yet, so this project sets the conventions (xUnit, no mocking library needed for a pure type).

## Out of Scope

- Migrating Topics, Tournaments, Participants, Topic authors, Matches and Auth to Results. Their order is decided after the Rounds slice (Q27). Their status-code corrections (including the remaining 404 → 403 changes) come with each migration (Q21).
- Showing server field errors next to individual Vue form fields (Q8).
- Machine-readable error codes and i18n (Q9).
- Chaining helpers: Map, Bind, Match, OnSuccess, task extensions (Q18).
- Integration or HTTP-level tests, a web application factory, Testcontainers (Q10, Q19).
- Logging expected failures (Q30).
- C# 15 union types. They need .NET 11, and the project targets .NET 10.
- Success responses that currently return a bare string body (for example after registration). Only failure responses are in scope.
- A domain glossary. Everything settled in this work is general programming vocabulary.

## Further Notes

- **Settled during spec writing, not asked in the grilling:**
  - GET-one-round is allowed for anyone who may read the tournament, using the same read permission as other tournament reads. Others get Forbidden.
  - The round service owns the transaction.
  - A successful `Result<T>` requires a non-null value.
  - Explicit success factories exist because of the interface-conversion limitation.
- **Check existing data before the unique-index migration.** The migration will fail if duplicate rounds already exist for the same match and round number.
- Status-code pages only add a body to error responses that have none, so the SPA fallback and successful responses are unaffected.
- **Later, when integration tests arrive:** they go in a separate project (web application factory plus Postgres), so that the fast unit tests can still run without Docker (Q19).

### Decision index (Q1–Q31)

| Q | Decision | Where in this spec |
|---|---|---|
| Q1 | Learning/portfolio project; the other project is a reference, not a template | Goals and scope |
| Q2 | Expected failures are Results; unexpected ones stay exceptions | Goals and scope; Layering |
| Q3 | Move rules from controllers to services gradually, Rounds first | Goals and scope; Slice 3 |
| Q4 | Every non-2xx response is RFC 9457 problem details; ships first | HTTP boundary; Slice 1 |
| Q5 | The old branch and its docs are reference only | Goals and scope |
| Q6 | Semantic failure kinds, not HTTP codes; one translation point | Failure model; HTTP boundary |
| Q7 | (Keep 404 for permission failures) **reversed by Q14** | Failure model |
| Q8 | Field-bound errors; per-field display deferred | Failure model; Frontend; Out of Scope |
| Q9 | No error codes for now | Failure model; Out of Scope |
| Q10 | Small test project for the Result type only | Testing Decisions; Slice 2 |
| Q11 | Slice 1 includes the frontend helper and unified envelope | Frontend; Slice 1 |
| Q12 | Write it ourselves; ADR 0001 | Goals and scope |
| Q13 | Five kinds with defined meanings; no extras | Failure model |
| Q14 | Honest status codes: permission failures are 403 | Failure model; Slice 3 table |
| Q15 | Permission checks move into services; controllers keep `[Authorize]` | Layering; Slice 3 |
| Q16 | Failure = kind + required message + optional field errors | Failure model |
| Q17 | Immutable sealed classes, implicit conversions, throwing accessors | Result type |
| Q18 | No chaining helpers until repetition shows | Result type; Out of Scope |
| Q19 | `Cbo.Results` without ASP.NET Core; single `Cbo.Tests` with a Results folder | Result type; Slice 2; Testing Decisions |
| Q20 | Problem details + exception handler + status-code pages; details only in Development | HTTP boundary; Slice 1 |
| Q21 | Slice 1 changes the format only; status fixes come with each migration | Slice 1; Out of Scope |
| Q22 | `ControllerBase.Problem(Failure)` via `ProblemDetailsFactory`; controllers choose the success response | HTTP boundary; Layering |
| Q23 | Repositories stay nullable; only services return Results | Layering |
| Q24 | Unique and foreign-key violations become Conflict in services | Layering; Slice 3 fix 2 |
| Q25 | Impossible states throw | Layering; Slice 3 table |
| Q26 | Rounds mapping table plus three fixes | Slice 3 |
| Q27 | Slice order 1 → 2 → 3; later features ordered after Rounds | Solution; Out of Scope |
| Q28 | Helper order: detail, field errors, title, fallback | Frontend |
| Q29 | camelCase JSON-path field keys; camelCase validation metadata provider | Failure model; HTTP boundary; Slice 1 |
| Q30 | Expected failures not logged | Layering; Out of Scope |
| Q31 | `result.md` deleted; the ADR is self-contained | (No effect on implementation) |
