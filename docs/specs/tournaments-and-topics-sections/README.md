# Tournaments and Topics sections

The home page stops being the place where everything happens. Tournaments and Topics each get their own list page, shown as a table. All per-item management (settings, participants, tournament topics, authors, delete) moves from dialogs onto the tournament page and the topic page.

Read before implementing any slice:

- [`GLOSSARY.md`](../../../GLOSSARY.md): use its terms in UI text, toasts and code comments (**Start**, **Topic**, **Owner**, **Author**, **Played**, **Bring**, **Tournament topic**, **Empty slot**).
- [ADR-0006](../../adr/0006-topic-authors-saved-with-the-topic.md): topic authors are saved with the topic.
- [ADR-0007](../../adr/0007-topic-minimum-is-informational.md): the topic minimum is informational.
- ADR-0002 to ADR-0004 if the slice touches the backend.

## Global constraints

These apply to every slice.

- **Prefer PrimeVue components** over hand-built markup: `Menubar`, `DataTable` (`@row-click`, `rowReorder` column), `Tabs`/`TabList`/`Tab`/`TabPanels`/`TabPanel`, `Tag`, `AutoComplete`, `ConfirmDialog` (`useConfirm`), `Message`, `Toast` (through `useNotify`).
- **Use `Tag` for every badge** in code you touch: stage, role, Played, Owner/Author. Replace `Badge` and the hand-rolled Tailwind badge `<span>`s.
- **No pagination and no sorting** on any table.
- **Toast wording** follows the existing convention: title `"<Noun> <past-tense verb>"` (for example `Topic created`), detail `"<title>" was <verb>.`
- **Service calls** follow the existing `{ success, data }` / `apiFailure` envelope. Show form errors with `toFormFailure` / `toFormErrors` from `src/utils/error.js`, and focus the first invalid field with `focusFirstInvalid`.
- **Validation** follows the existing eager pattern (`eager()` from `src/includes/validation.js`): a field is validated first on blur, then on every change while it's invalid.
- **Don't rename backend identifiers** (`ChosenTopic`, `TournamentTopic`, `PriorityIndex`, …). Only user-facing text has to follow the glossary.
- **Verify** with `npm run lint` and `npm run build` in `frontend/`, and with `dotnet build` and `dotnet test` in `backend/`.

## Slices

| Slice | Title | Depends on |
|-------|-------|------------|
| [B1](./b1-topic-limits.md) | Backend: informational minimum, maximum counts positions | none |
| [B2](./b2-authors-saved-with-topic.md) | Backend: topic authors saved with the topic | none |
| [F1](./f1-shared-building-blocks.md) | Frontend: shared building blocks (unsaved-changes guard, `TournamentForm`) | none |
| [F2](./f2-navigation-and-list-pages.md) | Frontend: navbar, Tournaments page, Topics page | F1 |
| [F3](./f3-topic-page.md) | Frontend: topic page (authors table, read-only, delete, drafts) | B2, F1 |
| [F4](./f4-tournament-page-shell-and-participants.md) | Frontend: tournament page header, tabs, Participants tab | none |
| [F5](./f5-tournament-settings-tab.md) | Frontend: Settings tab with Delete | F1, F4 |
| [F6](./f6-tournament-my-topics-tab.md) | Frontend: My Topics tab | B1, F2, F4 |
| [F7](./f7-home-page-and-cleanup.md) | Frontend: home page welcome and removal of old cards/dialogs | all others |

Suggested waves:

1. **B1, B2, F1, F4**: independent, so they can run in parallel.
2. **F2** (after F1) and **F3** (after B2 + F1).
3. **F5** (after F1 + F4) and **F6** (after B1 + F2 + F4). They fill different tab panels from F4, so they can run in parallel.
4. **F7**, last.

Notes:

- **B2 and F3 must land together.** B2 removes the author endpoints that the old authors dialog calls, and F3 removes that dialog.
- The old home page keeps working until F7, so no feature is unreachable between slices.
- F4 and F2 both add `stageTagSeverity` / `roleTagSeverity` to `src/utils/tournament.js` if the other hasn't landed yet. Whichever lands second reuses them.

## Decision log

Every question asked while designing this feature, with the final answer and the slice that implements it. Later answers override earlier ones where noted.

| # | Question | Decision | Slice |
|---|----------|----------|-------|
| Q1 | Home page content | Welcome heading for everyone. Logged in: "Welcome back, {username}". Guests: a short prompt to log in or register (replaces the lock screen). Nothing else for now; widgets may come later. | F7 |
| Q2 | Routes and navbar | Add login-only routes `/tournaments` (`tournaments`) and `/topics` (`topics`). The Menubar shows "Tournaments" and "Topics" only when logged in. **Don't** rename `topic-edit`: viewing and editing a topic are the same page. | F2 |
| Q3 | Tournaments table columns | Title · Stage (`Tag`) · My role · Created. No description, no row actions. | F2 |
| Q4 | Topics table columns | Title · Questions (count) · Authors (count) · I'm an author (✓) · Played (`Tag`). No description, no row actions. | F2 |
| Q5 | Row navigation | Clicking a row opens its page. The title cell is also a real `RouterLink`. | F2, F6 |
| Q6 | Creating a tournament | The form is extracted into a shared `TournamentForm`, used by a create-only dialog (the "New" button on the Tournaments page) and by the Settings tab. After creating, go to the new tournament's page. | F1, F2, F7 |
| Q7 | Topic save flow | "New" on the Topics page → `/topics/new`. Creating replaces the URL with the new topic's page. Saving changes stays on the page with a toast. Cancel → `/topics`. | F2, F3 |
| Q8 | Topic page layout | Authors wait for Save, like questions. Order: Details → Questions → Authors → button row. The button row has **Delete** on the left and **Cancel/Save** on the right. There is no separate Danger Zone section. Delete is hidden on `/topics/new`. | F3 |
| Q9 | Played topic | Everything is read-only through one `readOnly` flag that disables every input. Delete is disabled, with the explanation "Played topics can't be deleted." | F3 |
| Q10 | Tabs after Start | The tabbed section shows in every stage. After Start it sits below the standings and matches and is read-only. ~~Danger Zone as its own tab~~: superseded by Q36/Q39. | F4 |
| Q11 | Settings for non-Creators | The tab is visible but read-only. | F5 |
| Q12 | Participants tab | The dialog's structure becomes a panel: the add-participant row (Creator, Preparations), a Staff table and a Players table. Dragging Players changes seeding and saves at once. | F4 |
| Q13 | Saving tournament topics | Drop the minimum check and enforce the maximum. Save after every change; no Save button. | B1, F6 |
| Q14 | Topics table reuse and drag | One `TopicsTable` serves the Topics page and the My Topics tab. Rows are clickable in both. A drag-handle icon at the start of each row marks where to drag. | F2, F6 |
| Q15 | Start button | A Start button (`pi-play`) next to the stage tag in the tournament header, for the Creator during Preparations. It keeps the existing confirmation and reloads the page data in place. | F4 |
| Q16 | Reusable `DangerZone.vue` | ~~Reusable component~~: superseded by Q8 and Q39. No `DangerZone` component; both Delete buttons sit in their page's button row. Delete shows only on existing items and goes to the list page after deleting. | F3, F5 |
| Q17 | Name for a topic in a tournament | **Tournament topic**; the verb is **bring**. Avoid assigned, chosen, assignment. The tab is labelled "My Topics". | Glossary, B1, F6 |
| Q18 | Name for the placeholder | **Empty slot**. Avoid placeholder. | Glossary, F6 |
| Q19 | "Play button" | It is the existing **Start** action. | Glossary, F4 |
| Q20 | Where the minimum applies | Nowhere: not when saving tournament topics, not at Start. | B1 |
| Q21 | How authors are saved | Atomically with the topic: create and update requests carry the co-author list, and the author endpoints are removed. When a save fails, every edit stays on the page. | B2, F3 |
| Q22 | Authors on `/topics/new` | Shown. Co-authors can be added before the topic exists. | F3 |
| Q23 | Authors table | Username · Role (`Tag`s) · remove. The owner row can't be removed. A username field and Add button above the table add a row locally; the server checks it on Save. The "I am the Author" checkbox stays in Details and drives the owner row's Author tag live. | F3 |
| Q24 | Leave prompt on topic page | Ask before leaving the page with unsaved changes. | F1, F3 |
| Q25 | Read-only flags | Topic page: read-only when played. Settings: read-only for non-Creators or after Start. Participants: read-only for non-Creators or after Start. My Topics: read-only after Start. | F3, F4, F5, F6 |
| Q26 | Danger Zone tab | ~~4th tab~~: superseded by Q36/Q39 (Delete lives in the Settings tab). | F5 |
| Q27 | Immediate saves and trailing empty slots | Keep the local list after a successful save instead of rebuilding it from the response. An empty slot at the end shows the hint "saved once a topic follows". If a save fails: toast, then reload from the server. | F6 |
| Q28 | What the maximum counts | Tournament topics + empty slots. | B1, F6 |
| Q29 | Who can add empty slots | Everyone (Players too). | F6 |
| Q30 | Drag and click | Dragging starts only from the handle. My Topics rows open the topic, but empty-slot rows don't. Players rows aren't clickable (there's no participant page), and their handle shows only when seeding can change. | F4, F6 |
| Q31 | Draft backup | The topic form's unsaved state is kept in `sessionStorage` and restored with a banner offering Discard. A `beforeunload` prompt is added as well. | F1, F3 |
| Q32 | Minimum in the UI | The setting stays in the form with the hint "For information only"; min ≤ max is still validated. Topics columns show "N / max". | B1, F1, F4 |
| Q33 | Backend enforces positions | Reject `priorityIndex` ≥ max (and < 0). | B1 |
| Q34 | PrimeVue components | Use them wherever possible; `Tag` replaces `Badge` and hand-made badges in touched code. | all |
| Q35 | Selected tab in URL | No. The page always opens on the first tab. | F4 |
| Q36 | Settings tab saving | Save + Reset. Ask before leaving the page or switching tabs with unsaved settings. Saving updates the header and shows a toast. **Settings is the last tab and holds Delete**, which everyone sees but only the Creator can use. | F5 |
| Q37 | Recording the PrimeVue preference | No `AGENTS.md`; it is saved in engram memory. | n/a |
| Q38 | Tab order | Participants → My Topics → Settings. Opens on Participants. | F4 |
| Q39 | Delete in Settings | Delete on the left; Reset/Save on the right. Disabled for non-Creators, with the tooltip "Only the creator can delete this tournament." Enabled for the Creator in every stage. After deleting, go to `/tournaments`. | F5 |
| Q40 | ADRs | ADR-0006 (authors saved with the topic) and ADR-0007 (minimum is informational). | done |

## Original request → slices

| # | Request | Slices |
|---|---------|--------|
| 1 | Home page shows only a welcome message | F7 |
| 2 | Tournaments and Topics sections, linked from the navbar | F2 |
| 3 | Tables instead of cards | F2 (cards removed in F7) |
| 4 | Clicking a row opens the tournament/topic page | F2 |
| 5 | Authors become a table on the topic page, under Questions; the authors dialog goes away | B2, F3, F7 |
| 6a | Tournament page tabs (settings, participants, tournament topics as a reorder table with add topic and empty slots); Start near the title | B1, F4, F5, F6 |
| 6b | Delete for tournament and topic at the bottom (reworked: in the topic's button row and in the Settings tab) | F3, F5 |
| 7 | No pagination or sorting | all (global constraint) |
