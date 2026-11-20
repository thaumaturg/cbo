# Friend list

Working spec. It's deleted once the feature ships, so **don't link to this file from code, comments, commits or other docs**. The lasting decisions are in `docs/adr/0009-only-friends-can-be-authors-or-participants.md` and the term **Friend** in `GLOSSARY.md`.

## Problem

Users find it awkward to add topic co-authors and tournament participants. Today both are added by typing an exact username into a plain text box (`frontend/src/views/TopicView.vue`, `frontend/src/components/TournamentParticipantsPanel.vue`). There is no account search, and we are **not** adding a search over all accounts.

## Solution in one paragraph

Each account gets a **friend list**. A topic owner picks co-authors, and a tournament creator picks participants, from their friends with an autocomplete. The server only accepts friends as **newly added** co-authors and participants. There is one exception: an owner who isn't an author of their topic may still add any account by exact username. For now the list is one-sided. You add an account without asking it, it isn't notified, and it can't see that you added it.

## User stories

1. As an account, I can open a **Friends** page and see my friends, sorted by username.
2. As an account, I can add a friend by typing their exact username, ignoring case.
3. As an account, I can remove a friend, after confirming.
4. As a topic owner, I can pick co-authors from my friends with an autocomplete, without typing a full username.
5. As a tournament creator, I can pick participants (Organizer or Player) from my friends with an autocomplete.
6. As a topic owner or tournament creator, I can add a new friend from the picker without leaving the page, and they are selected straight away.
7. As a topic owner who has unticked "I am an author", I can still add any account as a co-author by exact username, because I'm entering a topic someone else wrote.
8. As an existing user, my friend list is already filled when the feature launches: my topics' co-authors and my tournaments' participants are on it.

## Rules

**Friend list**

- **R1**: The friend list belongs to one account, and is one-sided: it goes from the account to its friend, with no consent and no notification.
- **R2**: You can't add yourself, a username that has no account, or someone who is already your friend. A blank username is an error. Usernames are matched exactly, ignoring case (the same normalised lookup as today).
- **R3**: There is no size limit.

**Who can be added**

- **R4**: Co-authors you add to a topic must be on the **owner's** friend list.
  - Unless `isAuthor` is false in the same save (the exception in story 7).
- **R5**: Participants you add to a tournament, in either role (Organizer or Player), must be on the **creator's** friend list.
  - Only the creator can manage participants, so in practice the creator is always the caller.
  - There is no exception here.
- **R6**: R4 and R5 apply only to **newly added** people.
  - **Topics:** "newly added" means a co-author who isn't already one of this topic's co-authors (on create, every co-author is new).
  - **Existing people stay:** co-authors and participants who are already on a topic or tournament but aren't friends keep their place. Saving the topic again doesn't fail because of them.
  - **Ticking "I am an author" later:** if the owner ticks it after adding non-friends, those co-authors stay.
- **R7**: Removing a friend doesn't remove them from any topic or tournament.

**Out of scope**

- **R8**: Nothing else changes. Participants are still added only in the Preparations stage, the Creator role can't be given, duplicates are still refused, and so on.

## API

All endpoints are under `/api` and require authentication, following the existing endpoint group conventions.

### `GET /api/Friends`

- **Response:** `200` with `FriendResponse[]`, sorted by username, ignoring case.
- **Not paged.** This is a deliberate exception to ADR-0008.

```json
[{ "accountId": "guid", "username": "bob", "addedAt": "2026-11-20T10:00:00Z" }]
```

### `POST /api/Friends`

- **Body:** `{ "username": "bob" }`
- **Success:** `201` with the new `FriendResponse`.
- **Field errors** on `username` (the same 400 validation shape as other field errors):
  - Blank: "Username is required."
  - No account: "There's no account with the username 'bob'."
  - Yourself: "You can't add yourself as a friend."
  - Already a friend: "'bob' is already on your friend list."
    - Also returned when a simultaneous add loses the race on the unique index, through `MapSaveFailure`, as `CreateParticipantCommandHandler` does.

### `DELETE /api/Friends/{accountId}`

- **Success:** `204`.
- **Not found:** `404` if that account isn't on the caller's friend list.

### Existing endpoints

Request and response shapes don't change. The only change is these new field errors:

- **`POST /api/Topics`, `PUT /api/Topics/{id}`:** on `authors[i].username`, "'bob' is not on your friend list." Returned when R4 fails.
- **`POST /api/Tournaments/{tournamentId}/participants`:** on `username`, "'bob' is not on your friend list." Returned when R5 fails.

## Data

- **Entity:** `Friendship` in `backend/Cbo.Core/Domain`, implementing `IAuditable`, in table `friendships`. Fields:
  - `Id` (`uuidv7()` default)
  - `ApplicationUserId`: the account whose list this is
  - `FriendId`: the friend's account
  - `CreatedAt`, `UpdatedAt`
- **Navigations:** `ApplicationUser` and `Friend`, plus a `Friendships` collection on `ApplicationUser`.
- **Index:** unique on (`ApplicationUserId`, `FriendId`), with its name exposed as a constant like `TopicAuthorConfiguration.TopicIdApplicationUserIdIndexName`.
  - This follows the codebase convention of a surrogate `Id` plus a unique index, instead of the composite key discussed during design.
- **Foreign keys:** both point to `AspNetUsers` with cascade delete.
- **Check constraint:** `application_user_id <> friend_id`.
- **Configuration:** `backend/Cbo.Core/Data/Configuration/FriendshipConfiguration.cs`, written like the other configurations.
- **Migration:** one EF migration that creates the table and then fills it (story 8) with raw SQL, roughly:

  ```sql
  INSERT INTO friendships (id, application_user_id, friend_id, created_at)
  SELECT uuidv7(), pair.account_id, pair.friend_id, now()
  FROM (
      SELECT o.application_user_id AS account_id, c.application_user_id AS friend_id
      FROM topic_authors o
      JOIN topic_authors c ON c.topic_id = o.topic_id AND NOT c.is_owner
      WHERE o.is_owner
      UNION
      SELECT cr.application_user_id, p.application_user_id
      FROM tournament_participants cr
      JOIN tournament_participants p ON p.tournament_id = cr.tournament_id AND p.role <> 'Creator'
      WHERE cr.role = 'Creator'
  ) pair
  WHERE pair.account_id <> pair.friend_id
  ON CONFLICT DO NOTHING;
  ```

  Check the column names against `CboDbContextModelSnapshot.cs` before relying on this. `Down` drops the table.

## UI

**Header and Friends page**

- **Header:** add a "Friends" link in `AppHeader`, shown only when logged in, pointing to the new route `/friends` (`requiresAuth`).
- **Friends page** (`frontend/src/views/FriendsView.vue`):
  - a username box with an "Add" button (Enter also adds)
  - field errors from the server shown under the box
  - the friend list sorted by username, with a client-side filter box
  - a remove button with a confirm dialog
  - an empty state ("No friends yet")

**`AddFriendDialog`** (shared component)

- A small dialog with a username box. It calls `POST /api/Friends`, shows field errors, and on success emits the new friend and closes.

**`FriendPicker`** (shared component)

- A PrimeVue `AutoComplete` with `dropdown` and `forceSelection` over the caller's friends.
- **Loading and filtering:** it loads the friends once and filters them in the browser, matching anywhere in the username and ignoring case. This is the pattern in `TournamentTopicsPanel.vue`.
- **Inputs:** an `excludeAccountIds` (or usernames) prop hides people already added.
- **Output:** it emits the picked friend, and the parent adds the row.
- **"Add a friend" link:** next to the box, it opens `AddFriendDialog`. A friend added there is added to the loaded list and emitted as picked.

**Topic page** (`TopicView.vue`)

- The co-author username box and "Add" button are replaced by `FriendPicker`.
- Author rows, per-row server errors and everything else stay as they are.
- While "I am an author" is unticked, a second line appears: "Not on your friend list? Add by username", with the old username box and "Add" button (the same local checks as today).

**Participants panel** (`TournamentParticipantsPanel.vue`)

- The username box is replaced by `FriendPicker`. The role `Select` and the submit button stay.
- It submits the picked friend's username to the unchanged `POST .../participants`.

## Tests

- **Scope:** backend unit tests only, in the existing style: xUnit v3, sentence-style names, hand-written fakes. There are no handler or endpoint tests, and there is no frontend test runner.
- **What to test:** the friend rule (R4 + R6 + the exception) as pure logic. Put it in `TopicValidationService.ValidateCoAuthors`, which is already pure, by adding:
  - the owner's friend ids
  - the existing co-author ids
  - the owner's `isAuthor` flag

  Cover these cases:
  - a new co-author who is a friend is accepted
  - a new co-author who isn't a friend is rejected
  - an existing co-author who isn't a friend is accepted
  - a non-friend is accepted when `isAuthor` is false
  - the existing errors (blank, duplicate, unknown, owner) still win over the friend error
- **Friend-add checks:** if they become a pure function (blank, self, already a friend), unit-test them too.

## Later (not in this work)

- **Mutual friendships:** friend requests, accept and decline, an inbox or badge, and a symmetric list. The ADR's exception for owners who aren't authors needs another look then.
- **Worked-out "people you've worked with" list:** built from co-authors and fellow participants, shown as a second group in `FriendPicker`.
- **MultiSelect for authors:** a `MultiSelect` on the topic page for adding several co-authors at once, if one at a time feels slow.

---

## Slices

Each slice is a separate agent task and a separate commit (or a few). Give the agent this whole file and point it at its slice.

```
S0 ─────────────────────────────── (independent)
S1 ──┬── S2 ───────────┐
     └── S3 ──┬── S4 ──┼── merge S2 no earlier than S4 and S5
              └── S5 ──┘
```

**Rules for every agent:**

- **Fit in:** match the surrounding code: CQRS handlers without a mediator (ADR-0003), minimal API endpoint groups (ADR-0004), `Result` and `Failure` (ADR-0002), and the commit message style in `git log`.
- **Spec stays private:** don't reference this spec file anywhere.
- **Before finishing:** run `dotnet build` and `dotnet test` in `backend/` for backend slices, and `npm run lint` and `npm run build` in `frontend/` for frontend slices.
- **OpenAPI:** `backend/Cbo.Api/openapi/v1.json` is regenerated on build and committed. Commit the change if your slice changes it.

### S0: Remove the broken spec reference

- **Depends on:** nothing.
- **Do:** in `frontend/src/components/TournamentTopicsPanel.vue` (around line 78), remove or reword the comment that points to `docs/specs/list-pagination-and-filtering.md`, which no longer exists. Keep any explanation that's still useful without the link, for example the 100-row limit, which ADR-0008 also covers.
- **Done when:** no file in the repo mentions `docs/specs/`, apart from files inside `docs/specs/` itself.

### S1: Backend friend list

- **Depends on:** nothing.
- **Do:** follow the "Data" and the `GET`/`POST`/`DELETE /api/Friends` parts of "API" above.
  - **Entity, configuration, `DbSet`:** add `Friendship`, its configuration and the `DbSet`.
  - **Migration:** create the table and fill it from past work.
  - **Handlers and folder:** `GetFriendsQuery`, `AddFriendCommand`, `RemoveFriendCommand` with their handlers, request and response types, and failures, in a new `backend/Cbo.Core/Friends/` folder.
  - **Registration:** register the handlers as the other features do.
  - **Endpoints:** add `backend/Cbo.Api/Endpoints/FriendEndpoints.cs` and map it in `Program.cs`.
- **Tests:** unit tests for any pure validation you pull out.
- **Done when:**
  - the migration applies on a database with existing data and fills the table as described
  - the three endpoints behave as specified (check by hand or with `.http` requests)
  - `v1.json` is updated

### S2: Backend friends-only rule

- **Depends on:** S1, for the `Friendship` entity.
- **Do:** apply R4–R7.
  - **Topics:** load the owner's friend ids and the topic's existing co-author ids in the create and update topic handlers. Pass them, with `isAuthor`, to `ValidateCoAuthors`, which adds the "not on your friend list" error after the existing checks.
  - **Participants:** in `CreateParticipantCommandHandler`, after the account lookup, return the field error on `username` if the account isn't on the caller's friend list. The caller is the creator, because only the creator holds `ManageParticipants`.
- **Tests:** the cases listed under "Tests" above.
- **Merge:** merge after S4 and S5, or together with them. Merged alone, the old screens would let users type non-friends and then reject them.

### S3: Frontend friend list and shared pieces

- **Depends on:** S1 for running it. It can be built at the same time against the API contract above.
- **Do:**
  - `frontend/src/services/friends-service.js` with `getFriends`, `addFriend`, `removeFriend`
  - the `/friends` route and `FriendsView.vue`
  - the header link
  - `frontend/src/components/AddFriendDialog.vue`
  - `frontend/src/components/FriendPicker.vue`, as described in "UI"
- **Do not:** touch `TopicView.vue` or `TournamentParticipantsPanel.vue`.
- **Done when:**
  - you can add, filter and remove friends on the Friends page, and server errors show
  - `FriendPicker` works when placed on a page: it filters, excludes people, and "Add a friend" adds and picks

### S4: Topic page picker

- **Depends on:** S3.
- **Can run in parallel with:** S5.
- **Do:** in `TopicView.vue`, replace the co-author username box with `FriendPicker`, excluding current co-authors. Add the "Not on your friend list? Add by username" line, shown only while "I am an author" is unticked, using the old box and its local checks.
- **Done when:**
  - picking a friend adds an author row
  - existing rows and per-row server errors still work
  - the username fallback appears and disappears with the checkbox

### S5: Participants panel picker

- **Depends on:** S3.
- **Can run in parallel with:** S4.
- **Do:** in `TournamentParticipantsPanel.vue`, replace the username box with `FriendPicker`, excluding current participants. Keep the role select and submit, and submit the picked friend's username.
- **Done when:**
  - adding an Organizer or a Player from friends works
  - "Add a friend" in the picker works
  - server errors still show
