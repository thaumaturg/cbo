# F1: Frontend: shared building blocks

**Depends on:** none
**Decisions:** Q6 (shared form), Q24 / Q31 / Q36 (leave prompts), Q32 (minimum hint), Q34.

## Goal

Two pieces that later slices share:

- an **unsaved-changes guard** (used by F3 and F5);
- a **`TournamentForm`** component (used by the create dialog and by F5's Settings tab).

## Context

- `frontend/src/components/TournamentDialog.vue` holds a VeeValidate form with these fields:
  - title, description;
  - `playersPerTournament` (disabled, fixed at 14);
  - `topicsPerParticipantMin`, `topicsPerParticipantMax`, with `lte_field` between them.

  It has create and edit modes, calls `tournamentService.createTournament` / `updateTournament`, and emits `tournament-created` / `tournament-updated`. `HomeView.vue` uses both modes until F7.
- Confirmations use PrimeVue `useConfirm()`. `<ConfirmDialog>` is mounted in `App.vue`.
- The API interceptor (`src/services/api-interceptors.js`) redirects to `login` with `query.sessionExpired = "true"` on a 401.

## Scope

### 1. `src/utils/unsaved-changes.js`: `useUnsavedChangesGuard(isDirty)`

`isDirty` is a ref or getter. The composable:

- **Leaving the route.** Registers `onBeforeRouteLeave` and `onBeforeRouteUpdate`. While `isDirty` is true it asks through `useConfirm`:
  - header: "Unsaved Changes"
  - message: "You have unsaved changes. Leave and discard them?"
  - reject button: "Keep Editing" (secondary, outlined)
  - accept button: "Discard Changes" (danger)

  It resolves the navigation with the answer.
- **Session expiry.** Skips the prompt when the target is the `login` route with `sessionExpired === "true"`. The user can't save anyway, and F3's draft covers this case.
- **Closing or reloading the tab.** Adds a `beforeunload` listener that calls `preventDefault()` while dirty, and removes it on unmount.
- **Returns** `confirmDiscard(): Promise<boolean>`, which shows the same dialog when dirty and resolves `true` at once when clean. F5 uses it to guard tab switches.
- **Callers** must clear their dirty state before they navigate on purpose, for example after creating a topic and replacing the route.

### 2. `src/components/TournamentForm.vue`

- Move the fields, validation rules, default values and the status/error `Message`s out of `TournamentDialog.vue`.
- **Props:**
  - `tournament` (object or null; null means create);
  - `readOnly` (boolean): disables every input and hides the submit button;
  - `submitLabel` (string).
- **Saving.** The form calls the create or update service itself and handles errors exactly as the dialog does today (`toFormFailure`, `setErrors`, `focusFirstInvalid`). It emits `saved(tournament)` on success.
- **Exposes** `reset()`, which restores the values from `tournament` (or the defaults), and a `dirty` boolean, through `defineExpose` and/or a `update:dirty` event.
- **Actions slot.** An `#actions` slot with props `{ submitting, dirty, reset }`, so each host lays out its own buttons:
  - the dialog: Cancel + Create/Save;
  - Settings (F5): Delete on the left, Reset + Save on the right.

  The submit button can stay inside the form as the default slot content, or come from the slot. Either way it must stay `type="submit"` inside the `VeeForm`.
- **Minimum hint (Q32).** Under the "Topics Per Participant Min" field, add the hint "For information only. Participants aren't required to bring this many." (small muted text, or `Message` severity `secondary`, variant `simple`). Keep the `lte_field` rule.

### 3. `TournamentDialog.vue`

Render `TournamentForm` inside the `Dialog` and keep both modes and both emits for now. F7 makes it create-only.

## Out of scope

- Any view other than what `TournamentDialog` needs to keep working.

## Acceptance criteria

- Creating and editing a tournament from the current home page works exactly as before, with the new hint visible.
- `useUnsavedChangesGuard` exists, as described above (exercised by F3 and F5).
- `npm run lint` and `npm run build` pass.
