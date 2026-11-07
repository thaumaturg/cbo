# F3: Frontend: topic page

**Depends on:** B2 (the request carries `authors`), F1 (`useUnsavedChangesGuard`)
**Decisions:** Q7, Q8, Q9, Q16 (superseded: inline Delete), Q21, Q22, Q23, Q24, Q25 (topic part), Q31, Q34.

> B2 removes the author endpoints that `TopicAuthorsDialog` uses, so **B2 and F3 must land together**.

## Goal

The topic page (`/topics/new` and `/topics/:id`, route names `topic-new` / `topic-edit`) becomes the single place to manage a topic:

- details, questions and **authors**, all saved by one Save button;
- a **Delete** button in the same button row;
- a **read-only** mode for played topics;
- **leave prompts**, and a **draft** that survives session expiry and reloads.

## Context

- `src/views/TopicView.vue`:
  - a `VeeForm` with title (VeeField), description, the "I am the Author" checkbox (`formData.isAuthor`), and a questions `DataTable` with eager per-cell validation and a paste import;
  - Save calls `topicService.createTopic` / `updateTopic`, then `router.push("/")`;
  - server field errors map through `toFormErrors` (title) and `toQuestionErrors` (`questions[i].*`).
- `TopicResponse`: `id`, `title`, `description`, `isPlayed`, `isAuthor`, `questions[]` (with `id`) and `authors[]` (`id`, `username`, `isOwner`, `isAuthor`).
- After B2, both create and update requests take `authors: [{ username }]` (**co-authors only**, never the owner). Errors come back as `authors[i].username`, where `i` is the index in the sent list. Create and update both return the full `TopicResponse`.
- Today `src/components/TopicAuthorsDialog.vue` and `src/services/topic-authors-service.js` manage authors through the endpoints B2 removes. `HomeView.vue` and `TopicCard.vue` open that dialog.
- `useAuthStore().userName` is the logged-in username.

## Scope

### Layout (Q8)

Order inside the form:

1. **Details** card;
2. **Questions** card;
3. **Authors** card;
4. status `Message`s;
5. **button row**: on the left, **Delete topic** (`pi pi-trash`, severity `danger`, outlined), shown only in edit mode. On the right, **Cancel** (→ `topics`) and **Save** ("Create Topic" / "Save Changes").

No separate Danger Zone section.

### Authors card (Q22, Q23)

- Shown on both `/topics/new` and `/topics/:id`.
- **Add row** above the table: an `InputText` "Username" and an **Add** button (`type="button"`). Enter in the input adds the row and must **not** submit the form (`@keydown.enter.prevent`). Local checks, shown as a `Message` under the input (severity `error`, variant `simple`):
  - empty: "Username is required.";
  - already in the table (case-insensitive): "This user is already in the list.";
  - the owner's name: "You're the owner of this topic already."

  The server checks that the account exists, on Save.
- **`DataTable`** with no sorting and no paginator:
  - **Username**;
  - **Role**: `Tag` "Owner" (severity `contrast`) on the owner row, and `Tag` "Author" (severity `info`) on co-author rows and on the owner row while `formData.isAuthor` is ticked (updates live);
  - **remove** button (`pi pi-trash`, text, danger) on co-author rows only.
- **Rows:** the owner comes first. On an existing topic that's the author with `isOwner`; on a new topic it's `authStore.userName`. Co-authors follow in their saved order, then in the order they were added.
- **Server errors** `authors[i].username` show under that co-author's username cell, the same way question cell errors do. Adding or removing a row clears the author errors.
- **Saving** sends `authors: coAuthors.map(a => ({ username: a.username }))`.

### Save flow (Q7, Q21)

- **Create succeeds:** toast "Topic created", remove the draft, mark the form clean, then `router.replace({ name: 'topic-edit', params: { id } })`. The page then shows the saved topic, with Delete available.
- **Update succeeds:** toast "Topic updated", stay on the page, and **repopulate the form from the response**, so new questions and authors get their ids and the next save doesn't recreate them. Mark clean and remove the draft.
- **Failure:** keep every edit. Show title, question and author field errors and the banner (`toFormFailure` with title, every `questions[i].*` field and every `authors[i].username` field as known fields). Focus the first invalid field.
- **Load failure** goes to `topics` (not `/`).

### Delete (Q8, Q16)

- Uses the existing confirmation (header "Delete Topic", accept "Delete", severity danger).
- On success: toast "Topic deleted", remove the draft, mark clean, then go to `topics`.
- On failure: `notify.error("Delete Failed", message)`.

### Read-only mode (Q9, Q25)

- `readOnly = isEditMode && topic.isPlayed`.
- Show a `Message` (severity `info`) at the top: "This topic has been played, so it can't be changed or deleted."
- In read-only mode:
  - every input is disabled: title, description, checkbox, every question cell;
  - add/remove question, the paste import and its hint, the authors add row and remove buttons are hidden;
  - Save is hidden, and Cancel becomes **Back**;
  - Delete is disabled, with the tooltip "Played topics can't be deleted." Wrap it in a `<span>` so the tooltip works on a disabled button.
- Implement it as one `readOnly` computed that every control reads, not scattered checks.

### Unsaved changes (Q24)

- `isDirty`: compare a snapshot of `{ formData, questions, coAuthors }` taken after load or save with the current state (serialize to compare).
- Call `useUnsavedChangesGuard(isDirty)` from F1. Cancel/Back is an ordinary navigation, so it prompts when dirty.
- Accepting the leave prompt removes the draft.

### Draft backup (Q31)

- **Key:** `cbo:topic-draft:{username}:{topicId or "new"}` in `sessionStorage`.
- **Writing:** while `isDirty`, write `{ formData, questions, coAuthors }` on every change (a deep watch is fine). Remove the key when the form becomes clean.
- **Restoring:** on load (after fetching the server topic, for edit mode), if a draft exists and the topic isn't read-only, apply it. Show a `Message` (severity `info`) at the top: "Unsaved changes from your last visit were restored." with a **Discard** button. Discard removes the draft and restores the server state (or the empty form on `/topics/new`).
- **Read-only topics:** ignore and remove any draft.
- **Session expiry:** the guard doesn't prompt and the draft stays, so after logging in again the redirect brings the user back to the restored edits.

### Removals (Q21)

- Delete `src/components/TopicAuthorsDialog.vue` and `src/services/topic-authors-service.js`.
- In `HomeView.vue` and `TopicCard.vue`, remove only the authors button, emit, handler and dialog. F7 removes the rest.

## Out of scope

- The Topics list page (F2), the home page (F7).

## Acceptance criteria

- On `/topics/new` you can add co-authors, and creating the topic saves them. You end on `/topics/{id}` with Delete visible.
- Changing questions and authors and pressing Save stores both together. A bad username shows next to its row, nothing is saved, and every edit is still on screen.
- Leaving with unsaved changes asks first. Reloading the tab triggers the browser prompt. After a reload or a session expiry and new login, the draft is restored with a Discard option.
- A played topic shows every field disabled, no Save, Back instead of Cancel, and a disabled Delete with the explanation.
- Deleting a topic goes to `/topics`.
- No references to `topic-authors-service.js` remain.
- `npm run lint` and `npm run build` pass.
