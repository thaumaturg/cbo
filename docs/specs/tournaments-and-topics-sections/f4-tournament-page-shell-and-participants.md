# F4: Frontend: tournament page header, tabs, Participants tab

**Depends on:** none (uses `stageTagSeverity` / `roleTagSeverity` from F2 if F2 has landed; otherwise add them to `src/utils/tournament.js` as described in F2 §3)
**Decisions:** Q10, Q12, Q15, Q19, Q25 (participants part), Q30 (Players table), Q32 (Topics column), Q34, Q35, Q38.

## Goal

The tournament page (`/tournaments/:tournamentId`, `TournamentView.vue`) gets:

- a **Start** button in its header;
- a **tabbed section** at the bottom: Participants → My Topics → Settings;
- the first tab, **Participants**, which replaces the participants dialog.

F5 and F6 fill the other two tabs.

## Context

- `TournamentView.vue`:
  - loads the tournament (`TournamentResponse`, including `currentUserRole`, `currentStage`, `playersPerTournament`, `topicsPerParticipantMax`);
  - after Start, it also loads matches and Players for `TournamentStandingsCard` and the `MatchCard` grid;
  - in Preparations it shows a "Tournament is in Preparations" message;
  - it uses `Badge` for the stage.
- Start today lives in `HomeView.vue` → `handleTournamentStart`:
  - a confirmation with header "Start Tournament", the message `Are you sure you want to start "{title}"? Its qualification matches will be generated.` and accept label "Start";
  - then `tournamentService.advanceStage(id, "Qualifications")`.
- `src/components/TournamentParticipantsDialog.vue` holds all participant logic:
  - fetch, add (username + role `Select`), remove (with confirmation);
  - Staff and Players lists, Players seeding by `rowReorder`, saved at once through `updateSeeding`;
  - `canManageParticipants = Creator && Preparations`.

  Leave the dialog file in place (home still uses it until F7); copy its logic into the new panel.
- Permissions: any participant can read participants; only the Creator can manage them, and only during Preparations.

## Scope

### Header (Q15, Q19, Q34)

- Title, then the stage as a `Tag` (`stageTagSeverity`) in every stage, then, **only for the Creator during Preparations**, a **Start** button (`pi pi-play`, severity `success`, label "Start").
- Start uses the confirmation above. On success:
  - `notify.success("Tournament started", "\"{title}\" has started.")`;
  - reload the tournament in place, then load matches and Players as the page already does after Start.

  On failure: `notify.error("Start Failed", message)`.
- Replace `Badge` with `Tag`.

### Page body

Keep what's there: the Preparations message, or the standings and matches. Add the tabbed section **below** it, in every stage (Q10).

### Tabs (Q35, Q38)

- PrimeVue `Tabs` with `v-model:value="activeTab"` (a ref in the view; default `"participants"`; not in the URL).
- `TabList`: **Participants** (`participants`), **My Topics** (`my-topics`), **Settings** (`settings`).
- Each `TabPanel` renders one component and gets `tournament` and `readOnly`:
  - `TournamentParticipantsPanel` (this slice);
  - `TournamentTopicsPanel`: create it as a stub with a muted "Coming soon" line (F6 replaces it);
  - `TournamentSettingsPanel`: create it as a stub (F5 replaces it).
- Keep `activeTab` in the view as a plain ref, so F5 can guard tab switches.
- Compute the read-only flags in the view and pass them down:

  | Panel | `readOnly` when |
  |-------|-----------------|
  | Participants | not (Creator **and** Preparations) |
  | My Topics | not Preparations |
  | Settings | not (Creator **and** Preparations) |

### `src/components/TournamentParticipantsPanel.vue` (Q12, Q25, Q30, Q32)

- Props: `tournament`, `readOnly`. Fetches on mount, and again when `tournament.id` or `tournament.currentStage` changes.
- **Add row** (hidden when `readOnly`): the same username `InputText`, role `Select` (Organizer/Player) and **Add** button as the dialog, with the same field-error handling.
- **Heading:** "Participants: {organizers} Organizers, {players} of {playersPerTournament} Players".
- **Staff `DataTable`** (Creator first, then Organizers by username). Columns:
  - Username;
  - Role (`Tag`, `roleTagSeverity`);
  - Topics ("N / {topicsPerParticipantMax}");
  - remove button (`pi pi-trash`) on non-Creator rows when not `readOnly`.
- **Players `DataTable`** (by seed, then username). Columns:
  - a `rowReorder` drag-handle column, **only when** not `readOnly` and there are 2+ Players;
  - Seed;
  - Player;
  - Topics ("N / {max}", Q32);
  - remove button when not `readOnly`.

  Rows aren't clickable and there's no row-click navigation (Q30). Show the hint "Drag rows by the handle to change bracket seeding." while reordering is possible. Seeding saves on every reorder, as the dialog does today.
- **Removing** keeps the existing confirmation and the "Participant removed" toast.
- **Empty state:** "No participants yet".
- Use `Tag` for roles, not hand-rolled spans.

## Out of scope

- Settings tab contents and Delete (F5); My Topics contents (F6); removing the dialogs (F7).

## Acceptance criteria

- The Creator sees Start next to the stage tag during Preparations, and nobody else does. Starting reloads the page in place, the tables turn read-only, and standings and matches appear.
- Tabs show in every stage, in the order Participants, My Topics, Settings, and open on Participants.
- The Creator can add, remove and re-seed during Preparations. Other roles, and everyone after Start, see the same tables without controls or drag handles.
- The Topics columns show "N / max".
- `npm run lint` and `npm run build` pass.
