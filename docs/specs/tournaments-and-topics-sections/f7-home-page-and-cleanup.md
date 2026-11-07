# F7: Frontend: home page welcome and cleanup

**Depends on:** F2, F3, F4, F5, F6 (everything the old home page did must already exist elsewhere)
**Decisions:** Q1, Q6 (create-only dialog), Q34 (no `Badge` left in touched code). Original request #1 and #3.

## Goal

The home page shows only a welcome message. The cards and dialogs that the new pages replaced are deleted.

## Context

- `HomeView.vue`:
  - lists tournaments and topics as cards;
  - hosts `TournamentDialog` (create and edit), `TournamentParticipantsDialog` and `TournamentTopicsDialog`;
  - handles start and delete for both.

  After F3, the authors dialog is already gone.
- `useAuthStore()` exposes `isAuthenticated` and `userName`.
- Auth pages are `login` and `register`; links keep the redirect with `keepRedirect` from `src/utils/redirect.js`, as `AppHeader.vue` does.

## Scope

1. **`HomeView.vue`** (Q1). Replace everything with a centred welcome:
   - **Everyone:** the heading "Welcome to Competitive Bracket Organizer" and a one-line description (reuse the glossary intro: "Organize tournaments in which participants bring topics and players compete in matches of rounds.").
   - **Logged in:** "Welcome back, {userName}".
   - **Guests:** "Log in or register to get started", with **Log in** and **Register** buttons (`RouterLink` + PrimeVue `Button`, like the header).
   - No lists, no fetching, no dialogs. Leave room for future widgets; don't build any.
2. **Delete** `src/components/TournamentCard.vue`, `TopicCard.vue`, `TournamentParticipantsDialog.vue` and `TournamentTopicsDialog.vue`.
3. **`TournamentDialog.vue` becomes create-only** (Q6):
   - remove the `mode` and `tournament` props and the `tournament-updated` emit;
   - header "Create New Tournament";
   - it renders `TournamentForm` with `tournament = null`.

   Update `TournamentsView.vue` if it passes `mode`.
4. **Sweep:** `grep` for the deleted components, `topic-authors-service`, `Badge` imports and `stageBadgeSeverity`, and remove or replace anything left with `Tag` / `stageTagSeverity`.

## Acceptance criteria

- `/` shows only the welcome content, for guests and for logged-in users.
- No imports of the deleted files remain, and `Badge` isn't used in any file touched by this feature.
- Creating a tournament from `/tournaments` still works.
- `npm run lint` and `npm run build` pass.
