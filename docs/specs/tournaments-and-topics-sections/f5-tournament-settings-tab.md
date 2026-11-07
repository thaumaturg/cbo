# F5: Frontend: Settings tab with Delete

**Depends on:** F1 (`TournamentForm`, `useUnsavedChangesGuard`), F4 (tabs shell, `TournamentSettingsPanel` stub, `activeTab`)
**Decisions:** Q6 (shared form), Q11, Q16 / Q26 (superseded), Q25 (settings part), Q32, Q36, Q39.

## Goal

The last tab, **Settings**, shows the tournament's settings form:

- editable by the Creator during Preparations, and read-only for everyone else;
- **Delete tournament** on the left of its button row, which everyone sees but only the Creator can use.

## Context

- F1's `TournamentForm`:
  - props `tournament`, `readOnly`, `submitLabel`;
  - emits `saved(tournament)`;
  - exposes `reset()` and `dirty`;
  - has an `#actions` slot with `{ submitting, dirty, reset }`;
  - the minimum field already carries the "For information only" hint.
- F1's `useUnsavedChangesGuard(isDirty)` handles route leaves and `beforeunload`, and returns `confirmDiscard()`.
- F4's `TournamentView.vue` owns `tournament`, `activeTab` and the read-only flags. It renders `TournamentSettingsPanel` with `tournament` and `readOnly` (= not Creator, or not Preparations).
- Delete today is `HomeView.vue` → `handleTournamentDelete`: a confirmation ("Delete Tournament", ``Are you sure you want to delete "{title}"?``, accept "Delete", severity danger), then `tournamentService.deleteTournament(id)`. The backend lets the Creator delete in **any** stage.

## Scope

### `src/components/TournamentSettingsPanel.vue`

Replaces F4's stub.

- Renders `TournamentForm` with `:tournament`, `:read-only="readOnly"` and `submit-label="Save"`.
- **Button row** (through the `#actions` slot):
  - **Left: Delete tournament** (`pi pi-trash`, severity `danger`, outlined).
    - **Visible to everyone, in every stage.** Enabled only when `tournament.currentUserRole === 'Creator'`.
    - When disabled, it shows the tooltip "Only the creator can delete this tournament." Wrap it in a `<span>` so the tooltip works on a disabled button.
    - It stays enabled for the Creator after Start, even though the form is read-only.
  - **Right: Reset** (secondary, outlined; disabled when not dirty) and **Save** (`type="submit"`; disabled when not dirty or while submitting). Both are hidden when `readOnly`.
- **On `saved`:** emit `updated(tournament)` to the view, and `notify.success("Tournament updated", "\"{title}\" was updated.")`.
- **Delete:** confirm as above. On success:
  - `notify.success("Tournament deleted", "\"{title}\" was deleted.")`;
  - mark the form clean, so the guard doesn't prompt;
  - `router.push({ name: 'tournaments' })`.

  On failure: `notify.error("Delete Failed", message)`.
- Emit the form's dirty state to the view (for example `update:dirty`).

### `TournamentView.vue`

- On `updated`, replace `tournament` with the returned one, so the header title and description update. The form then rebases on the new values and is clean.
- Call `useUnsavedChangesGuard(settingsDirty)` for route leaves and tab close (Q36).
- **Guard tab switches (Q36):** bind `Tabs` to `:value="activeTab"` with an `@update:value` handler. When leaving `settings` with unsaved settings, `await confirmDiscard()`. If the user accepts, reset the form and switch; otherwise stay on Settings.

## Out of scope

- A separate Danger Zone tab or component (superseded by Q36/Q39).
- Draft backup for settings (only the topic page has drafts).

## Acceptance criteria

- The Creator in Preparations can edit and save settings. The header updates and a toast appears. Reset discards the edits.
- Organizers and Players, and everyone after Start, see the form disabled with no Reset or Save.
- Everyone sees Delete. Only the Creator can use it, in any stage, and it ends on `/tournaments`. Others get the disabled button with its tooltip.
- With unsaved settings, switching tabs, navigating away or closing the tab asks first.
- `npm run lint` and `npm run build` pass.
