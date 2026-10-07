# ADR 0001: Result pattern for operation outcomes

- Status: accepted
- Date: 2026-09-23

## Context

Application services expressed outcomes ad hoc: `string?` for a single validation message,
`null` for "not found", `Dictionary<string, string[]>` for field errors. Controllers held the
business rules and translated each outcome to an HTTP response by hand, so the same rule was
often encoded twice and error bodies differed between endpoints. Unexpected exceptions produced
an empty 500 in production.

## Decision

Introduce `Cbo.Results`, a dependency-free library with an immutable `Result` / `Result<T>`,
a semantic `ResultStatus` (`Ok`, `Created`, `NoContent`, `Invalid`, `NotFound`, `Conflict`,
`Unauthorized`, `Forbidden`, `Unexpected`) and `Error(Identifier, Message, Code?)` where
`Identifier` is the camelCase JSON member path. Services return `Result`; a single MVC adapter
(`ToActionResult`) maps it to HTTP, emitting RFC 9457 problem details through
`ProblemDetailsFactory`. A global `IExceptionHandler` turns anything that escapes into a generic
RFC 9457 response, so every non-2xx response has the same shape.

### Wire contract for failures

- Errors with an empty `Identifier` (operation-level) are joined into `detail`.
- Errors with an `Identifier` (member-level) go into the `errors` map of
  `ValidationProblemDetails`, so clients can bind them to form fields. Without any, a plain
  `ProblemDetails` is returned.
- Distinct `Error.Code`s are listed in the `errorCodes` extension member. Codes follow
  `<entity>.<camelCaseReason>` (e.g. `round.alreadyExists`), are defined in one catalogue per
  feature (`RoundErrors`) and are part of the API contract. Errors a client cannot act on
  (`Unexpected`) and the deliberately opaque access-check 404 carry no code.
- The frontend interprets this contract in exactly one place, `toFailure` in
  `frontend/src/utils/error.js`; every service returns
  `{ success: false, error: string, status, fieldErrors, errorCodes }`, so components never
  see a raw response body.

### Alternatives considered

- **ErrorOr** — mature discriminated union, but no first-class field identifier on `Error`
  (would live in `Metadata`), and the ASP.NET Core adapter is a low-adoption third-party package.
- **Ardalis.Result** — closest match (semantic status, `ValidationError.Identifier`), but the
  official adapter returns `BadRequest(validationErrors)` rather than ProblemDetails, and
  `Identifier` exists only for the `Invalid` status.
- **FluentResults** — widely used, with the richest composition API of the three
  (async `Bind`, implicit conversions, `Merge`, `FailIf`, causal error chains, metadata). But a
  result is failed whenever it holds any `IError` and has no failure category, so mapping to
  404/409 requires custom `Error` subclasses and type-sniffing (`HasError<NotFoundError>()`) in a
  custom profile. Its ASP.NET Core extension's default profile returns 400 with a
  `[{ "message" }]` array for every failure, not ProblemDetails. Results are mutable
  (`Reasons` is a public `List<IReason>`; `WithError`/`WithValue` mutate in place), and
  behaviour is configured through static global state (`Result.Setup`).

All three would still have required a custom adapter to keep the ProblemDetails contract the
frontend already consumes. The surface this application needs is about 200 lines, and
demonstrating the design (immutability, invariants enforced at construction, layering) is a goal
of this repository.

### Deliberately not adopted from these libraries

- Converting exceptions into results (`Result.Try`): it blurs the line this ADR draws between
  expected outcomes and bugs/outages, which belong to the global exception handler.
- Global mutable configuration (`Result.Setup`) and mutable results.
- Success "reasons"/messages: `Created` + `Location` cover the only success detail the API needs.
- Implicit `Error` → `Result` conversion: an `Error` carries no `ResultStatus`, so the conversion
  would have to guess one.

## Consequences

- Exceptions are reserved for unexpected failures; expected failures are values.
- Controllers become one-line adapters; business rules live in services.
- `Cbo.Results` must stay free of package references and of any ASP.NET Core dependency.
- Remaining controllers migrate incrementally, following `RoundService` /
  `TournamentsController.Rounds.cs` as the reference: `RoundErrors` catalogue,
  `[ProducesResponseType]` metadata so the OpenAPI document shows the error contract, and a
  `CancellationToken` passed from the action down to EF Core (commits deliberately ignore it, so a
  disconnecting client cannot leave the commit outcome unknown).
- Until then, non-migrated endpoints still answer some failures with a plain-string body;
  `toFailure` accepts both shapes.
- Semantic statuses mean uncommon HTTP codes (e.g. 429) need a new enum member and adapter row
  when they are first needed; this is intentional.

## Future work

Worth borrowing from the alternatives, each added only when real code needs it:

- **Async combinators** — `Bind`/`Map` over `Task<Result<T>>` (as in FluentResults and ErrorOr), so
  multi-step async service flows can chain instead of repeating `if (x.IsFailure) return x.As<T>();`.
- **Implicit `T` → `Result<T>` conversion** — lets a service write `return dto;` for a plain
  success. Safe because a value always means `Ok`; the reverse (`Error` → `Result`) stays excluded.
- **Error-code query** — e.g. `result.HasCode("round.alreadyExists")`, the code-based counterpart of
  FluentResults' `HasError<T>()`, for callers that branch on a specific failure.
