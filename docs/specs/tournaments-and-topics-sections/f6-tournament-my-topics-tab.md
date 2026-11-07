# F6: Frontend: My Topics tab

**Depends on:** B1 (no minimum; `priorityIndex < max`), F2 (`TopicsTable`), F4 (tabs shell, `TournamentTopicsPanel` stub)
**Decisions:** Q5, Q13, Q14, Q17, Q18, Q25 (My Topics part), Q27, Q28, Q29, Q30, Q34.

## Goal

The **My Topics** tab shows the topics the current participant brings to this tournament (their **tournament topics**, see `GLOSSARY.md`) as a reorderable `TopicsTable`:

- adding a topic, adding an **empty slot**, removing and reordering each **save at once**;
- clicking a row opens the topic.

## Context

- `src/components/TournamentTopicsDialog.vue` has the current logic. Copy what's useful; the file is deleted in F7.
  - `getMyTopics` returns `{ id, topicId, topicTitle, priorityIndex }[]`.
  - `buildListFromDtos` rebuilds empty slots from the gaps in `priorityIndex`.
  - `setMyTopics(tournamentId, [{ topicId, priorityIndex }])` saves the whole list.
  - An `AutoComplete` searches owned topics that aren't in the list yet.
  - It uses `OrderList` and an explicit Save button. Both go away.
- After B1:
  - no minimum anywhere;
  - the server rejects `priorityIndex < 0` or `>= topicsPerParticipantMax` (`[i].priorityIndex`), and more than max topics;
  - empty slots aren't stored, so an empty slot at the **end** of the list can't be saved.
- `topicService.getAllTopics()` returns the current user's topics with `questions`, `authors`, `isAuthor` and `isPlayed`. Every tournament topic is one of them.
- F2's `TopicsTable` (list mode):
  - columns Title (link) · Questions · Authors · I'm an author · Played;
  - `@row-click` → `topic-edit`.

## Scope

### Extend `TopicsTable.vue` (Q14, Q30)

Add an opt-in reorder mode without changing list mode. Suggested props:

- `reorderable`: adds a leading `rowReorder` column (PrimeVue's drag-handle icon; dragging starts only from the handle) and emits `reorder(newRows)`.
- `removable`: adds a trailing remove button column (`pi pi-times`, text, danger) that emits `remove(index)`.
- `showPosition`: adds a "#" column with `index + 1`.
- `dataKey`: defaults to `"id"`; this tab uses `"key"`.
- `disabled`: shows the table's `loading` state and disables the handles and remove buttons.

**Rows** can be topic rows (the usual fields plus `topicId`) or **empty-slot rows** (`{ key, isEmptySlot: true }`):

- An empty-slot row shows "Empty slot" in the Title cell (muted italic, or a `Tag` severity `secondary`). Its other data cells are empty.
- An empty slot with no topic after it adds the muted hint "Saved once a topic follows" (Q27).
- **Clicking a row:** topic rows open `topic-edit` for `topicId` (or `id` in list mode). Empty-slot rows do nothing.

### `src/components/TournamentTopicsPanel.vue`

Replaces F4's stub. Props: `tournament` and `readOnly` (= not Preparations).

- **Load** on mount: `getMyTopics` and `getAllTopics` in parallel. Build the list with leading and middle empty slots from `priorityIndex` gaps, and join each tournament topic with its owned topic by `topicId` to fill the table columns.
- **Status line**, a `Message`:
  - "{list length} / {max} positions used". This counts tournament topics + empty slots (Q28).
  - Severity `info`, or `warn` when full.
  - Add "Suggested minimum: {min} (not required)." in muted text (Q32, ADR-0007).
- **Add row** (hidden when `readOnly`):
  - an `AutoComplete` "Search your topics…" (`dropdown`, `forceSelection`) over owned topics not yet in the list. Its `#empty` slot says either "All your topics are already in this tournament" or "No matching topics found".
  - an **Add empty slot** button (`pi pi-minus-circle`, secondary, outlined) for **every role** (Q29), with the tooltip "Reserve a priority position without a topic".
  - Both are disabled when the list length is `>= max`, with the note "Maximum reached ({max})" (Q28).
- **Table:** `TopicsTable` with `reorderable`, `removable`, `showPosition` and `dataKey="key"`. In `readOnly` mode it has no handle, no remove and no add row, but rows still open their topic.
- **Saving on every change (Q13, Q27):**
  - Each add, add-empty-slot, remove or reorder updates the local list, then saves.
  - The payload is `list.map((row, i) => ({ row, i })).filter(not empty slot).map(({ row, i }) => ({ topicId: row.topicId, priorityIndex: i }))`.
  - While a save is in flight, pass `disabled` to the table and disable the add controls, so saves never overlap.
  - **Success:** keep the local list as it is. **Don't** rebuild it from the response; that keeps a trailing empty slot visible. Don't show a toast; this matches seeding.
  - **Failure:** `notify.error("Save Failed", message)`, then reload the list from the server.
  - Adding an empty slot at the end changes nothing on the server. It may skip the request, or send it anyway; either is fine.
- **Wording (Q17, Q18):** say "topics you bring", "My Topics" and "Empty slot". Never say "assigned", "chosen" or "placeholder".

## Out of scope

- Storing empty slots on the server.
- Deleting `TournamentTopicsDialog.vue` (F7).

## Acceptance criteria

- During Preparations, adding, removing, reordering by the handle and adding an empty slot each save at once. A page reload shows the same list, except a trailing empty slot, which carried the "Saved once a topic follows" hint.
- A Player can save any number of topics from 0 up to max. The list (topics + empty slots) can't grow beyond max.
- Every role sees "Add empty slot".
- Clicking a topic row opens that topic. Empty-slot rows aren't clickable.
- After Start, the tab is read-only, but rows still open their topic.
- `npm run lint` and `npm run build` pass.
