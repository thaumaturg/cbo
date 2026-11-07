# B1: Backend: informational minimum, maximum counts positions

**Depends on:** none
**Decisions:** Q13, Q17 (wording), Q20, Q28, Q32 (backend part), Q33. See [ADR-0007](../../adr/0007-topic-minimum-is-informational.md).

## Goal

Tournament topics save after every edit in the frontend (F6), so the backend must accept lists below the topic minimum. The maximum is enforced on priority positions, which counts tournament topics and the empty slots between them.

## Context

- `backend/Cbo.Core/TournamentTopics/SetMyTournamentTopicsCommandHandler.cs`:
  - Rejects a Player's list below `TopicsPerParticipantMin` ("You must choose at least …").
  - Rejects more than `TopicsPerParticipantMax` topics.
  - Validates topic ids per item, with field errors `[{i}].topicId`.
- `backend/Cbo.Core/Tournaments/StartTournamentCommandHandler.cs` → `CheckTopicCounts` rejects Start when a Player has fewer than the minimum, and when anyone has more than the maximum.
- Empty slots aren't stored. They are gaps in `PriorityIndex` (0-based) between a participant's tournament topics. An empty slot at the end of the list is never sent.
- `TournamentSettings` validates min ≤ max. That stays (Q32).

## Scope

1. **SetMyTournamentTopicsCommandHandler**
   - Remove the minimum check.
   - Keep the count ≤ max check.
   - Add a per-item check: `0 <= PriorityIndex < TopicsPerParticipantMax`. Otherwise return the field error `[{i}].priorityIndex` with "The priority position must be between 1 and {max}." Collect these field errors together with the existing `topicId` ones, so all of them come back in one `Failure.Invalid`.
   - User-facing wording follows the glossary verb **bring**:
     - `ChooseTopicsForbiddenMessage` → "You don't have permission to bring topics to this tournament."
     - "Some of the chosen topics are invalid." → "Some of the tournament topics are invalid."
     - "You can choose at most {max} topics." → "You can bring at most {max} topics."
     - "You can only choose topics you own." → "You can only bring topics you own."
2. **StartTournamentCommandHandler.CheckTopicCounts**: remove the Player minimum check, keep the maximum check, and update the XML doc comment.
3. Update any doc comment that describes the minimum as enforced (for example on `Tournament.TopicsPerParticipantMin` or `DefaultSettings`, if present) to say it's informational.
4. Don't rename identifiers (`ChosenTopic`, `ChosenTopicRequest`, …).

## Out of scope

- Storing empty slots.
- Frontend changes (F6 consumes this).

## Acceptance criteria

- A Player can save 0, 1, … topics up to the maximum.
- Saving an item with `priorityIndex` = max, or one below 0, returns 400 with the field error `[i].priorityIndex`.
- A tournament with a Player who has fewer topics than the minimum can be started.
- A participant over the maximum still blocks Start. Count > max on save is still rejected.
- `dotnet build` and `dotnet test` pass.
