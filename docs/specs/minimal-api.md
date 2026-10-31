# Spec: move the API from controllers to Minimal APIs

Status: agreed, not started. Decision record: [ADR 0004](../adr/0004-minimal-apis-instead-of-controllers.md).

## Goal

Replace the MVC controllers in `backend/Cbo.API/Controllers` with Minimal API endpoints. The Vue app keeps seeing exactly the same API: same routes, verbs, status codes and bodies. API changes are made only after the swap, each in its own commit (see [API changes after the swap](#api-changes-after-the-swap)).

## What stays as it is

- Command and query handlers, the `CommandHandler` base class and `AddCqrsHandlers` ([ADR 0003](../adr/0003-cqrs-without-a-mediator.md)).
- `Cbo.Results` and the meaning of each failure kind ([ADR 0002](../adr/0002-hand-rolled-result-with-failure-kinds.md)).
- Resource-based authorization inside handlers (`TournamentAccessService`, `TopicAccessService`, the authorization handlers in `Authorization/`). Endpoints only require a signed-in user.
- `AuthService` stays a service; the auth endpoints call it directly.
- After a command, the endpoint runs the matching query and returns the resource (201 + DTO for create, 200 + DTO for update and start). The Vue app uses these bodies.
- `GlobalExceptionHandler` keeps answering unexpected exceptions with a 500 problem response and client aborts with a bodiless 499.
- `UseStatusCodePages` keeps giving bodiless errors (such as the JWT challenge 401) a problem body.

## Design

### Pipeline (`Program.cs`)

- JSON: `builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()))`. Minimal APIs read `Microsoft.AspNetCore.Http.Json.JsonOptions`, not the MVC options. While controllers and endpoints coexist, both are configured.
- Bad requests: `builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true)` in every environment (see [Malformed requests](#malformed-requests)).
- Endpoints are mapped where `app.MapControllers()` is today, after `UseAuthorization`:

  ```csharp
  RouteGroupBuilder api = app.MapGroup("/api").RequireAuthorization();
  api.MapAuthEndpoints();
  api.MapTopicEndpoints();
  // ...
  ```

- No `FallbackPolicy`. It would also cover the `MapFallbackToFile("index.html")` endpoint and break deep links into the SPA in production.
- No permission policies, no endpoint logging filter, no custom execution metrics. The built-in request logs and the `http.server.request.duration` metric cover timing.

### Failure → HTTP

`ErrorHandling/FailureHttpExtensions.cs` replaces `ControllerBaseExtensions.cs` and is the single place where a `Failure` becomes an HTTP response:

```csharp
public static IResult ToProblem(this Failure failure)
```

- Status from the kind: Invalid 400, NotFound 404, Conflict 409, Forbidden 403, Unauthorized 401. An unknown kind throws.
- No field errors: `TypedResults.Problem(detail: failure.Message, statusCode: status)`.
- Field errors: an `HttpValidationProblemDetails` whose `errors` groups messages by `FieldError.Field`, with `Status = status` and `Detail = failure.Message`, returned through `TypedResults.Problem(problemDetails)`. The kind's status is kept, so field errors can come with any kind (Register answers a taken email with 409 and field errors). `TypedResults.ValidationProblem` is not used because it is always 400.
- `ProblemHttpResult` writes through `IProblemDetailsService`, so `type`, `title` and `traceId` are filled in as today.

While controllers still exist, `ControllerBaseExtensions` stays; it is deleted with the last controller.

### Malformed requests

Controllers answered malformed JSON, missing `required` properties and unknown enum values with an automatic 400. Minimal APIs don't, and without changes Development would return a 500 (the binding exception reaches `GlobalExceptionHandler`) and Production a bodiless 400.

`GlobalExceptionHandler` gets a branch, after the client-abort check:

- `BadHttpRequestException` → the exception's `StatusCode` (400, or 415 for a wrong content type), a problem body with a generic title, and the exception message as `detail` only in Development (it names internal C# types).
- Logged at Information, not Error.

The body no longer has an `errors` map keyed by JSON paths such as `$.title`. The Vue form helpers can't map those keys to fields anyway, so the user sees the same thing: a general error message on the form.

### Endpoint classes

One static class per current controller file, in `Endpoints/`:

| Class | Replaces | Route prefix (under `/api`) | Tag |
|---|---|---|---|
| `AuthEndpoints` | `AuthController` | `Auth` | `Auth` |
| `TopicEndpoints` | `TopicsController` | `Topics` | `Topics` |
| `TopicAuthorEndpoints` | `TopicsController.Authors` | `Topics/{topicId:guid}/authors` | `Topics` |
| `TournamentEndpoints` | `TournamentsController` | `Tournaments` | `Tournaments` |
| `TournamentParticipantEndpoints` | `TournamentsController.Participants` | `Tournaments/{tournamentId:guid}/participants` | `Tournaments` |
| `TournamentMatchEndpoints` | `TournamentsController.Matches` | `Tournaments/{tournamentId:guid}/matches` | `Tournaments` |
| `TournamentRoundEndpoints` | `TournamentsController.Rounds` | `Tournaments/{tournamentId:guid}/matches/{matchId:guid}/rounds` | `Tournaments` |
| `TournamentTopicEndpoints` | `TournamentsController.Topics` | `Tournaments/{tournamentId:guid}/topics` | `Tournaments` |

Route prefixes keep the current PascalCase during the swap so the OpenAPI comparison stays clean.

Conventions:

- Each class has one `public static RouteGroupBuilder Map…Endpoints(this RouteGroupBuilder api)` that creates its group with `MapGroup(prefix).WithTags(tag)` and maps its endpoints.
- Endpoints point to named `private static async Task<IResult>` methods, not inline lambdas.
- Every handler or service parameter has `[FromServices]`. Without it, a missing registration silently turns the parameter into a body parameter.
- Response metadata is copied one-to-one from the controller attributes: `.Produces<T>(status)`, `.Produces(status)`, `.ProducesProblem(status)`, `.ProducesValidationProblem(status)`.
- Every endpoint has a unique `.WithName(...)`, verb + noun (see the inventory). It becomes the OpenAPI `operationId` and the route name used for `Location` headers.
- 201 responses use `TypedResults.CreatedAtRoute(dto, "<GetEndpointName>", new { ... })`, which builds the same absolute `Location` URL as `CreatedAtAction`.
- Register and Login get `.AllowAnonymous()`; everything else inherits the sign-in requirement of the `/api` group.

Example:

```csharp
public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder tournaments = api.MapGroup("Tournaments").WithTags("Tournaments");

        tournaments.MapGet("{id:guid}", GetTournamentAsync)
            .WithName("GetTournament")
            .Produces<GetTournamentDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tournaments.MapPost("", CreateTournamentAsync)
            .WithName("CreateTournament")
            .Produces<GetTournamentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return api;
    }

    private static async Task<IResult> GetTournamentAsync(
        Guid id,
        [FromServices] IQueryHandler<GetTournamentQuery, GetTournamentDto> getTournament,
        CancellationToken cancellationToken)
    {
        Result<GetTournamentDto> result = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateTournamentAsync(
        CreateTournamentDto createTournamentDto,
        [FromServices] ICommandHandler<CreateTournamentCommand, Guid> createTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, GetTournamentDto> getTournament,
        CancellationToken cancellationToken)
    {
        var command = new CreateTournamentCommand(/* ... from createTournamentDto ... */);

        Result<Guid> created = await createTournament.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return created.Failure.ToProblem();

        Result<GetTournamentDto> tournament = await getTournament.HandleAsync(new GetTournamentQuery(created.Value), cancellationToken);
        if (tournament.IsFailure)
            return tournament.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(tournament.Value, "GetTournament", new { id = tournament.Value.Id });
    }
}
```

### Endpoint inventory

Routes are relative to `/api`. Success status and body as today.

| Name | Verb | Route | Success |
|---|---|---|---|
| `Register` | POST | `Auth/Register` | 200 `text/plain` (anonymous) |
| `Login` | POST | `Auth/Login` | 200 `LoginResponseDto` (anonymous) |
| `ChangePassword` | POST | `Auth/ChangePassword` | 204 |
| `GetTopics` | GET | `Topics` | 200 `List<GetTopicDto>` |
| `GetTopic` | GET | `Topics/{id}` | 200 `GetTopicDto` |
| `CreateTopic` | POST | `Topics` | 201 `GetTopicDto` |
| `UpdateTopic` | PUT | `Topics/{id}` | 200 `GetTopicDto` |
| `DeleteTopic` | DELETE | `Topics/{id}` | 204 |
| `GetTopicAuthors` | GET | `Topics/{topicId}/authors` | 200 `List<GetTopicAuthorDto>` |
| `GetTopicAuthor` | GET | `Topics/{topicId}/authors/{id}` | 200 `GetTopicAuthorDto` |
| `CreateTopicAuthor` | POST | `Topics/{topicId}/authors` | 201 `GetTopicAuthorDto` |
| `DeleteTopicAuthor` | DELETE | `Topics/{topicId}/authors/{id}` | 204 |
| `GetTournaments` | GET | `Tournaments` | 200 `List<GetTournamentDto>` |
| `GetTournament` | GET | `Tournaments/{id}` | 200 `GetTournamentDto` |
| `CreateTournament` | POST | `Tournaments` | 201 `GetTournamentDto` |
| `UpdateTournament` | PUT | `Tournaments/{id}` | 200 `GetTournamentDto` |
| `DeleteTournament` | DELETE | `Tournaments/{id}` | 204 |
| `StartTournament` | PATCH | `Tournaments/{id}` | 200 `GetTournamentDto` |
| `GetTournamentParticipants` | GET | `Tournaments/{tournamentId}/participants?role=` | 200 `List<GetTournamentParticipantDto>` |
| `GetTournamentParticipant` | GET | `Tournaments/{tournamentId}/participants/{id}` | 200 `GetTournamentParticipantDto` |
| `CreateTournamentParticipant` | POST | `Tournaments/{tournamentId}/participants` | 201 `GetTournamentParticipantDto` |
| `UpdateTournamentParticipant` | PUT | `Tournaments/{tournamentId}/participants/{id}` | 200 `GetTournamentParticipantDto` |
| `UpdateTournamentSeeding` | PUT | `Tournaments/{tournamentId}/participants/seeding` | 200 `List<GetTournamentParticipantDto>` |
| `DeleteTournamentParticipant` | DELETE | `Tournaments/{tournamentId}/participants/{id}` | 204 |
| `GetTournamentMatches` | GET | `Tournaments/{tournamentId}/matches` | 200 `List<GetMatchDto>` |
| `GetTournamentMatch` | GET | `Tournaments/{tournamentId}/matches/{matchId}` | 200 `GetMatchDetailDto` |
| `GetMatchAvailableTopics` | GET | `Tournaments/{tournamentId}/matches/{matchId}/available-topics` | 200 `List<GetAvailableTopicDto>` |
| `GetMatchRound` | GET | `Tournaments/{tournamentId}/matches/{matchId}/rounds/{roundNumber}` | 200 `GetRoundDto` |
| `CreateMatchRound` | POST | `Tournaments/{tournamentId}/matches/{matchId}/rounds` | 201 `GetRoundDto` |
| `UpdateMatchRound` | PUT | `Tournaments/{tournamentId}/matches/{matchId}/rounds/{roundNumber}` | 200 `GetRoundDto` |
| `DeleteMatchRound` | DELETE | `Tournaments/{tournamentId}/matches/{matchId}/rounds/{roundNumber}` | 204 |
| `GetMyTournamentTopics` | GET | `Tournaments/{tournamentId}/topics` | 200 `List<GetTournamentTopicDto>` |
| `GetAllTournamentTopics` | GET | `Tournaments/{tournamentId}/topics/all` | 200 `List<GetTournamentTopicDto>` |
| `SetMyTournamentTopics` | PUT | `Tournaments/{tournamentId}/topics` | 200 `List<GetTournamentTopicDto>` |
| `GetTournamentTopic` | GET | `Tournaments/{tournamentId}/topics/{topicId}` | 200 `GetTopicDto` |

Failure responses: copy each action's `[ProducesProblem]` / `[ProducesValidationProblem]` attributes.

### Port the controller code, not only the attributes

Each endpoint method does what its controller action does today. In particular, keep:

- Route constraints exactly as in the controllers (`{id:guid}`, `{roundNumber:int}`, …). The inventory omits them for brevity.
- Query parameters, such as the optional `role` filter (`TournamentParticipantRole?`) on `GetTournamentParticipants`.
- The DTO → command mapping, field by field.
- The follow-up query after a command, and which query it is (for example `UpdateTournamentSeeding` returns only the players).
- Logic that lives in the controller: `StartTournament` rejects any stage other than `Qualifications` with an Invalid failure on the `stage` field before calling the handler.

## Migration plan

Each step is one commit. Controllers and endpoints coexist until the last step.

0. **Prerequisite: the API starts.** In `Program.cs`, only when `builder.Environment.IsDevelopment()`, add `options.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))` to the `AddDbContext` options, with a comment that it is removed together with the next migration. Commit it as `chore(backend): log pending model changes in Development until the next migration`. See [Running the API](#running-the-api).
1. **Groundwork.**
   - `FailureHttpExtensions` with unit tests.
   - `ConfigureHttpJsonOptions` and `ThrowOnBadRequest`.
   - The `BadHttpRequestException` branch in `GlobalExceptionHandler`.
   - The `/api` group in `Program.cs`, with no endpoints yet.

   Before changing anything, save the baselines (see [Verification](#verification)).
2. **Auth.** `AuthEndpoints`; delete `AuthController`.
3. **Topics.** `TopicEndpoints`, `TopicAuthorEndpoints`; delete both `TopicsController` files.
4. **Tournaments.** The five tournament endpoint classes; delete the five `TournamentsController` files.
5. **Cleanup.** Remove:
   - `AddControllers` (with `SystemTextJsonValidationMetadataProvider` and the MVC `AddJsonOptions`) and `MapControllers`;
   - `ControllerBaseExtensions`, `ProducesProblemAttribute` and `ProducesValidationProblemAttribute`;
   - the `Controllers/` folder.

## How to implement

### Rules for implementing agents

- **One agent per step, in order.** Every step edits `Program.cs` and builds on the previous one. Don't start a step before the previous step's commit has been reviewed.
- **One commit per step**, in the repository's message style (`git log --oneline`), for example `refactor(backend): move auth endpoints to minimal APIs`. Don't push.
- **Stop and report, don't improvise**, when:
  - the OpenAPI comparison shows a difference not listed in [Allowed OpenAPI differences](#allowed-openapi-differences);
  - the smoke test fails;
  - existing tests fail;
  - the spec doesn't cover a case.

  Report what you saw and what you'd propose, and leave the step uncommitted.
- Follow the code style of the surrounding files: `ImplicitUsings` is disabled, so add the `using` directives; use explicit types for `Result` values as the controllers do.

### Running the API

- Postgres runs through Docker Compose: `docker compose up -d db` from the repository root (needs `.env`, see `README.md`).
- Start the API with the `http` profile, which avoids the HTTPS certificate setup: `dotnet run --project backend/Cbo.API --launch-profile http`. It listens on `http://localhost:5100` and serves `/openapi/v1.json` in Development.
- Startup applies migrations, so the API only starts when the model and the migrations agree.

> **Pending model changes:** commits `7d8c957` and `150d19e` removed database defaults from the EF configuration without adding a migration, so startup throws `PendingModelChangesWarning`. The migration is deliberately postponed: the owner adds one migration for all model changes later. **Agents never add migrations.** Until then, step 0 turns the error into a logged warning in Development only. Running like this is safe: EF now sets the ids and values the database used to default, and the unused database defaults don't change inserts. Production still refuses to start without the migration.

### Verification

Every step, before its commit:

1. `dotnet build backend/backend.slnx` with no new warnings.
2. `dotnet test backend/backend.slnx` passes.
3. The OpenAPI comparison shows only allowed differences.
4. The smoke test passes.
5. Nothing in the migrated area still uses a controller, `ControllerBaseExtensions` or the `Produces*Attribute` classes.

#### OpenAPI comparison

```bash
# Baseline, once, before step 1, with the API running:
curl -s http://localhost:5100/openapi/v1.json | python3 -m json.tool --sort-keys > /tmp/opencode/openapi-before.json

# After each step, with the API restarted:
curl -s http://localhost:5100/openapi/v1.json | python3 -m json.tool --sort-keys > /tmp/opencode/openapi-after.json
diff -u /tmp/opencode/openapi-before.json /tmp/opencode/openapi-after.json
```

##### Allowed OpenAPI differences

- `operationId` values added by `.WithName`.
- The validation problem schema named `HttpValidationProblemDetails` instead of `ValidationProblemDetails`, with the same properties.
- Content types narrowed to `application/json`: controllers also list `text/json` (and `text/plain` for strings) for responses, and `text/json` / `application/*+json` for request bodies. Minimal APIs list only `application/json`. Problem responses stay `application/problem+json`.
- Register's 200 response listed as `text/plain` only.

#### Unit tests

`Cbo.Tests/ErrorHandling/FailureHttpExtensionsTests.cs`. `Cbo.Tests` is a plain .NET project, so add both `<FrameworkReference Include="Microsoft.AspNetCore.App" />` and a project reference to `Cbo.API`. Package versions live in `backend/Directory.Packages.props`.

For each of the five kinds, with and without field errors, assert on the returned `ProblemHttpResult`:
- the status code and `detail`;
- with field errors, an `HttpValidationProblemDetails` whose `errors` group the messages by field (two errors on one field end up in one array).

#### Smoke test

Copy the script to `/tmp/opencode/smoke.sh` and run `bash /tmp/opencode/smoke.sh` with the API running. It needs `curl` and `python3`, and creates two users, a tournament and a topic in the local database. Run it once on the baseline before step 1: if it fails there, stop and report, because the script itself is wrong.

The API changes after the swap update the script in the same commit (lowercase paths, the new start call, Register's 204).

```bash
#!/usr/bin/env bash
# Smoke test for the Cbo API. Exits on the first failed check.
set -euo pipefail

BASE="${BASE:-http://localhost:5100/api}"
RUN="$(date +%s)"
PASSWORD="Smoke-test-123"
OUT=/tmp/opencode/smoke
mkdir -p "$OUT"

# call METHOD PATH [BODY] [TOKEN] sets STATUS, TYPE, BODY and LOCATION
call() {
  local method=$1 path=$2 body=${3-} token=${4-} result
  local args=(-s -X "$method" "$BASE$path" -o "$OUT/body" -D "$OUT/headers" -w '%{http_code} %{content_type}')
  if [[ -n $body ]]; then args+=(-H 'Content-Type: application/json' --data "$body"); fi
  if [[ -n $token ]]; then args+=(-H "Authorization: Bearer $token"); fi
  result=$(curl "${args[@]}")
  STATUS=${result%% *}
  TYPE=${result#* }
  BODY=$(cat "$OUT/body")
  LOCATION=$( (grep -i '^location:' "$OUT/headers" || true) | cut -d' ' -f2- | tr -d '\r')
}

# check STATUS NAME [PYTHON_CONDITION on b, the parsed body]
check() {
  local expected=$1 name=$2 condition=${3-}
  if [[ $STATUS != "$expected" ]]; then
    echo "FAIL $name: expected $expected, got $STATUS ($TYPE): $BODY"; exit 1
  fi
  if [[ -n $condition ]] && ! python3 -c "import json, sys; b = json.loads(sys.argv[1]); sys.exit(0 if ($condition) else 1)" "$BODY"; then
    echo "FAIL $name: body doesn't satisfy '$condition': $BODY"; exit 1
  fi
  echo "ok   $name"
}

# problem STATUS NAME [PYTHON_CONDITION]: like check, and the body must be problem details
problem() {
  if [[ $TYPE != application/problem+json* ]]; then
    echo "FAIL $2: expected problem details, got $STATUS ($TYPE): $BODY"; exit 1
  fi
  check "$1" "$2" "b['status'] == $1${3:+ and ($3)}"
}

field() { python3 -c "import json, sys; print(json.loads(sys.argv[1])$1)" "$BODY"; }

OWNER="smoke_owner_$RUN"
PLAYER="smoke_player_$RUN"
MISSING_ID="00000000-0000-0000-0000-000000000001"

# Auth
call POST /Auth/Register "{\"email\":\"$OWNER@example.com\",\"password\":\"$PASSWORD\",\"username\":\"$OWNER\"}"
check 200 "register owner"
call POST /Auth/Register "{\"email\":\"$PLAYER@example.com\",\"password\":\"$PASSWORD\",\"username\":\"$PLAYER\"}"
check 200 "register player"
call POST /Auth/Register "{\"email\":\"$OWNER@example.com\",\"password\":\"$PASSWORD\",\"username\":\"$OWNER\"}"
problem 409 "register a taken account keeps field errors on 409" "len(b['errors']) > 0"
call POST /Auth/Login "{\"email\":\"$OWNER@example.com\",\"password\":\"wrong-password\"}"
problem 401 "log in with a wrong password"
call POST /Auth/Login "{\"email\":\"$OWNER@example.com\",\"password\":\"$PASSWORD\"}"
check 200 "log in" "isinstance(b['jwtToken'], str)"
TOKEN=$(field "['jwtToken']")

# Sign-in requirement
call GET /Tournaments
problem 401 "list tournaments without a token"

# Tournaments
call GET /Tournaments "" "$TOKEN"
check 200 "list tournaments" "isinstance(b, list)"
call POST /Tournaments "{\"title\":\"Smoke $RUN\"}" "$TOKEN"
check 201 "create tournament" "b['title'] == 'Smoke $RUN'"
[[ -n $LOCATION ]] || { echo "FAIL create tournament: no Location header"; exit 1; }
TID=$(field "['id']")
call GET "/Tournaments/$TID" "" "$TOKEN"
check 200 "get tournament" "b['id'] == '$TID'"
call PUT "/Tournaments/$TID" "{\"title\":\"Smoke $RUN renamed\"}" "$TOKEN"
check 200 "update tournament returns the resource" "b['title'] == 'Smoke $RUN renamed'"
call PUT "/Tournaments/$TID" "{\"title\":\"Smoke $RUN\",\"playersPerTournament\":3}" "$TOKEN"
problem 400 "update tournament with invalid settings" "'playersPerTournament' in b['errors']"
call GET "/Tournaments/$MISSING_ID" "" "$TOKEN"
problem 404 "get a missing tournament"

# Start (PATCH { stage } until the API change replaces it)
call PATCH "/Tournaments/$TID" "{\"stage\":\"Preparations\"}" "$TOKEN"
problem 400 "start with an unsupported stage" "'stage' in b['errors']"
call PATCH "/Tournaments/$TID" "{\"stage\":\"Qualifications\"}" "$TOKEN"
problem 409 "start without enough players"

# Participants (enums travel as strings)
call POST "/Tournaments/$TID/participants" "{\"username\":\"$PLAYER\",\"role\":\"Player\"}" "$TOKEN"
check 201 "add participant" "b['role'] == 'Player' and b['username'] == '$PLAYER'"
call GET "/Tournaments/$TID/participants?role=Player" "" "$TOKEN"
check 200 "list players" "len(b) == 1 and b[0]['role'] == 'Player'"

# Matches and tournament topics before the start
call GET "/Tournaments/$TID/matches" "" "$TOKEN"
check 200 "list matches" "b == []"
call GET "/Tournaments/$TID/topics" "" "$TOKEN"
check 200 "list my tournament topics" "isinstance(b, list)"

# Topics
QUESTION='{"questionNumber":1,"costPositive":10,"costNegative":-10,"text":"Question","answer":"Answer"}'
call POST /Topics "{\"title\":\"Smoke topic $RUN\",\"isAuthor\":true,\"questions\":[$QUESTION]}" "$TOKEN"
check 201 "create topic" "b['title'] == 'Smoke topic $RUN' and len(b['questions']) == 1"
TOPIC=$(field "['id']")
QID=$(field "['questions'][0]['id']")
call PUT "/Topics/$TOPIC" "{\"title\":\"Smoke topic $RUN renamed\",\"isAuthor\":true,\"questions\":[{\"id\":\"$QID\",\"questionNumber\":1,\"costPositive\":20,\"costNegative\":-20,\"text\":\"Question\",\"answer\":\"Answer\"}]}" "$TOKEN"
check 200 "update topic returns the resource" "b['title'] == 'Smoke topic $RUN renamed'"
call POST /Topics "{\"title\":\"Bad topic\",\"isAuthor\":true,\"questions\":[{\"questionNumber\":1,\"costPositive\":10,\"costNegative\":-10,\"text\":\"\",\"answer\":\"Answer\"}]}" "$TOKEN"
problem 400 "create topic with an empty question" "'questions[0].text' in b['errors']"
call POST "/Topics/$TOPIC/authors" "{\"username\":\"$PLAYER\"}" "$TOKEN"
check 201 "add topic author" "b['username'] == '$PLAYER' and b['isOwner'] is False"
call DELETE "/Topics/$TOPIC" "" "$TOKEN"
check 204 "delete topic"

# Malformed requests
call POST /Tournaments '{"title":' "$TOKEN"
problem 400 "malformed JSON"
call POST /Tournaments '{}' "$TOKEN"
problem 400 "missing required property"
call POST "/Tournaments/$TID/participants" "{\"username\":\"$PLAYER\",\"role\":\"Boss\"}" "$TOKEN"
problem 400 "unknown enum value"

# Clean up
call DELETE "/Tournaments/$TID" "" "$TOKEN"
check 204 "delete tournament"

echo "All checks passed."
```

At the end of step 5, a person checks the Vue app by hand: sign in, create and edit a tournament and a topic, add a participant, start a tournament, play a round.

## API changes after the swap

Each is its own commit that also updates the Vue app and the smoke test, and is checked with the OpenAPI comparison:

1. **Lowercase routes**: `/api/tournaments`, `/api/topics`, `/api/auth/register`, and so on, in the endpoint classes and the Vue services.
2. **Start a tournament**: `POST /api/tournaments/{id}/start`, no body, 200 + `GetTournamentDto`. Remove the `PATCH` endpoint and `UpdateTournamentStageDto`. Rename `TournamentOperations.AdvanceStage` to `Start` and the Vue `advanceStage` to `startTournament`, matching the glossary term **Start**.
3. **Register returns 204**: the Vue app already shows its own success message and never reads the body.

## Out of scope

- Request validation with DataAnnotations or `AddValidation()`. Validation stays in handlers.
- Changing what commands return.
- Response compression, CORS changes, health checks.

## Future work

- [ ] **Remove the pending-model-changes workaround**: delete the Development-only `ConfigureWarnings(... PendingModelChangesWarning ...)` from `Program.cs` in the same commit that adds the migration for the removed database defaults.
- [ ] **Reorganize DTOs** (separate spec): `Models/DTO` holds 30 request and response shapes, all in use, and it is hard to see which belongs to which use case. Group by feature (request, command, handler, endpoint together) and name them `…Request` / `…Response`.
- [ ] **Integration tests** (separate spec): `WebApplicationFactory` with a real Postgres (Testcontainers), plus a convention test that every `/api` endpoint has a name, a tag, and either the sign-in requirement or `AllowAnonymous`. `Program.Main` runs `Database.Migrate()` at startup, which the test host has to account for.
- [ ] **Committed OpenAPI snapshot**: generate `openapi/v1.json` at build time (`Microsoft.Extensions.ApiDescription.Server`) so every API change shows up in diffs. Startup currently requires a connection string and a JWT key, which build-time generation would need to work around.
