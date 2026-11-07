# B2: Backend: topic authors saved with the topic

**Depends on:** none
**Decisions:** Q21, Q22, Q23 (server side). See [ADR-0006](../../adr/0006-topic-authors-saved-with-the-topic.md).

## Goal

Create-topic and update-topic carry the topic's full list of co-authors, and the handler saves it in the same transaction as the details and questions. The separate author endpoints go away.

## Context

- Topics: `backend/Cbo.Core/Topics/`
  - `CreateTopicRequest`, `UpdateTopicRequest`, `CreateTopicCommand`, `UpdateTopicCommand`, and their handlers.
  - Endpoints are in `backend/Cbo.Api/Endpoints/TopicEndpoints.cs`. Create and update return the `TopicResponse` from `GetTopicQuery`.
- Authors today:
  - Endpoints in `backend/Cbo.Api/Endpoints/TopicAuthorEndpoints.cs`, mapped in `Program.cs` via `api.MapTopicAuthorEndpoints()`.
  - Handlers: `GetTopicAuthors*`, `GetTopicAuthor*`, `CreateTopicAuthor*`, `DeleteTopicAuthor*`.
  - Also `CreateTopicAuthorRequest`, `TopicOperations.ManageAuthors` (and its use in `TopicAuthorizationHandler`), and `TopicFailures.PlayedAuthorsCannotChange`.
- `TopicAuthor` has `IsOwner` and `IsAuthor`. The table has a unique index on (TopicId, ApplicationUserId): `TopicAuthorConfiguration.TopicIdApplicationUserIdIndexName`.
- `TopicResponse.Authors` already returns all authors (owner included) with `username`, `isOwner` and `isAuthor`.
- Updating a played topic is already rejected with 409, so authors of a played topic can't change.
- Command handlers follow ADR-0003: one transaction per command, and `MapSaveFailure` for constraint violations.

## Scope

1. **Request shape.** Add a required `Authors` list to both `CreateTopicRequest` and `UpdateTopicRequest`:
   ```json
   "authors": [{ "username": "alice" }, { "username": "bob" }]
   ```
   - The list holds **co-authors only**. The owner is never in it; the owner's own flag is still `isAuthor`.
   - Carry it through the commands, for example as `IReadOnlyList<string> AuthorUsernames`, or as a small record if you prefer symmetry with questions.
2. **Validation.** Report everything as field errors `authors[{i}].username`, collected together with the question field errors into one `Failure.Invalid` (reuse `TopicFailures.Invalid`):
   - Empty or whitespace: "Username is required."
   - The same username appears earlier in the list (case-insensitive, compare normalized names): "This author appears more than once."
   - No such account: "User '{username}' doesn't exist."
   - The username is the owner's (the current user): "The owner can't be added as a co-author."
   - Look the accounts up in one query (by `NormalizedUserName`), not one `UserManager` call per row.
3. **Create.** Add one `TopicAuthor` per co-author with `IsOwner = false` and `IsAuthor = true`, next to the owner row that's already created.
4. **Update.** Sync the co-authors after the existing played check:
   - keep the rows of co-authors still in the list (ids stay stable);
   - remove co-author rows that are no longer in the list;
   - add rows for new usernames.

   Never touch the owner row, except for the existing `IsAuthor` update.
5. **Race.** In `MapSaveFailure`, map a unique violation on `TopicIdApplicationUserIdIndexName` to `Failure.Conflict("Someone changed this topic's authors at the same time. Reload the page and try again.")`.
6. **Remove**:
   - `TopicAuthorEndpoints.cs` and its mapping in `Program.cs`;
   - the four author query/command types and their handlers;
   - `CreateTopicAuthorRequest`;
   - `TopicOperations.ManageAuthors` and its branch in `TopicAuthorizationHandler`;
   - `TopicFailures.PlayedAuthorsCannotChange`, if nothing else uses it;
   - every DI registration of the removed handlers.

   Keep `TopicAuthorResponse` and `TopicAuthorProjections` (used by `TopicResponse`).
7. Update the doc comments that mention co-authors being added separately (for example on `CreateTopicCommandHandler`).

## Out of scope

- Frontend (F3 sends the list and removes `topic-authors-service.js`).
- Letting co-authors read or edit the topic. Only the owner still can.

## Acceptance criteria

- `POST /api/Topics` with `authors: [{ username: "bob" }]` creates the topic with the owner and bob (`isOwner=false`, `isAuthor=true`) in `authors`.
- `PUT /api/Topics/{id}` with a changed list adds and removes co-authors and leaves the owner untouched. The response reflects the new list.
- Bad rows come back as 400 with `authors[i].username` errors, alongside any `questions[i].*` errors. Nothing is saved.
- `GET|POST|DELETE /api/Topics/{id}/authors…` no longer exist (404).
- `dotnet build` and `dotnet test` pass.
