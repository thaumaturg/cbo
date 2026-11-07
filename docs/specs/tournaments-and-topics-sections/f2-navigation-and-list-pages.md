# F2: Frontend: navbar, Tournaments page, Topics page

**Depends on:** F1 (the create dialog uses `TournamentForm`)
**Decisions:** Q2, Q3, Q4, Q5, Q6 (create flow), Q7 (the "New" topic button), Q14 (`TopicsTable`, list mode), Q34. Global: no pagination, no sorting.

## Goal

Tournaments and Topics get their own login-only pages, linked from the navbar. Each lists its items in a PrimeVue `DataTable` instead of cards, and a row click opens the item's page.

## Context

- `src/router/index.js` has `home`, `login`, `register`, `profile`, `topic-new` (`/topics/new`), `topic-edit` (`/topics/:id`), `tournament-view` (`/tournaments/:tournamentId`) and `match-view`. Auth is enforced by `meta.requiresAuth`.
- `src/components/AppHeader.vue` renders a PrimeVue `Menubar` with an empty `items` array.
- `HomeView.vue` currently fetches the lists:
  - `tournamentService.getAllTournaments()` returns tournaments where you're a participant (`TournamentResponse`: `id`, `title`, `description`, `currentStage`, `createdAt`, …, `currentUserRole`);
  - `topicService.getAllTopics()` returns topics you own (`TopicResponse`: `id`, `title`, `description`, `isPlayed`, `isAuthor`, `questions[]`, `authors[]`).

  **Don't** change `HomeView.vue` in this slice; F7 strips it.
- `src/utils/tournament.js` has `stageBadgeSeverity(stage)`, which returns secondary/info/success. Those are valid `Tag` severities.

## Scope

1. **Routes.** Add `/tournaments` (name `tournaments`, `TournamentsView`) and `/topics` (name `topics`, `TopicsView`), both `meta: { requiresAuth: true }`. **Keep** the route name `topic-edit` (Q2).
2. **Navbar.** In `AppHeader.vue`, make the Menubar `model` a computed that's empty for guests and holds "Tournaments" (`pi pi-trophy`, route `tournaments`) and "Topics" (`pi pi-book`, route `topics`) when logged in.
   - Render the items with the Menubar `#item` slot and `RouterLink` (PrimeVue's router-link pattern), so they're real links.
   - Highlight the active section (for example when the current route starts with `/tournaments`).
3. **`src/utils/tournament.js`.** Add `roleTagSeverity(role)`: Creator → `contrast`, Organizer → `info`, Player → `success`. Rename `stageBadgeSeverity` to `stageTagSeverity` and update its callers (`TournamentView.vue`, `TournamentCard.vue`).
4. **`src/components/TournamentsTable.vue`.** Props: `tournaments`, `loading`. A `DataTable` with `dataKey="id"`, `rowHover`, a pointer cursor on rows, `@row-click` → `router.push({ name: 'tournament-view', params: { tournamentId } })`.
   - **Columns (Q3):**
     - **Title**: a `RouterLink` to the same page, with `@click.stop` so it doesn't navigate twice;
     - **Stage**: `Tag` with `stageTagSeverity`;
     - **My role**: `Tag` with `roleTagSeverity`;
     - **Created**: `createdAt` formatted with `toLocaleDateString()`.
   - No description, no row actions, no sorting, no paginator.
   - `#empty` slot: "No tournaments yet". Show loading with the `loading` prop.
5. **`src/components/TopicsTable.vue`.** Props: `topics`, `loading`. Same row-click and title-link pattern, going to `topic-edit`.
   - **Columns (Q4):**
     - **Title**;
     - **Questions**: `questions.length`;
     - **Authors**: `authors.length`;
     - **I'm an author**: a `pi pi-check` icon when `isAuthor`, empty otherwise;
     - **Played**: `Tag` "Played" (severity `success`) when `isPlayed`.
   - `#empty` slot: "No topics yet".
   - F6 adds a reorder mode to this component, so keep the column definitions easy to extend (for example a `mode` prop added later, or slots). Don't build the reorder mode here.
6. **`src/views/TournamentsView.vue`.**
   - Heading "Tournaments" and a "New" button (`pi pi-plus`, secondary, outlined), which opens `TournamentDialog` in create mode.
   - Fetch on mount. If the fetch fails, show `notify.error("Load Failed", message)`.
   - On `tournament-created`: `notify.success("Tournament created", "\"{title}\" was created.")`, close the dialog, then `router.push` to the new tournament's `tournament-view` (Q6).
7. **`src/views/TopicsView.vue`.** Heading "Topics" and a "New" button that goes to `topic-new` (Q7). Fetch on mount, with the same error handling.

## Out of scope

- The home page, removing the cards and dialogs (F7).
- What the topic page does after a save (F3).

## Acceptance criteria

- Logged in, the navbar shows Tournaments and Topics. Guests don't see them, and opening `/tournaments` or `/topics` as a guest redirects to login.
- Both pages show tables with exactly the columns above. Clicking a row or the title opens the item. Middle-clicking the title opens it in a new tab.
- Creating a tournament from `/tournaments` ends on the new tournament's page with a toast.
- "New" on `/topics` opens `/topics/new`.
- `npm run lint` and `npm run build` pass.
