# The topic minimum is informational

A tournament stores both a minimum and a maximum number of topics per participant. We stopped enforcing the minimum anywhere. Saving a participant's tournament topics doesn't check it, and Start doesn't check it either. It stays in the settings and is shown to participants as guidance only. We did this because tournament topics now save after every edit (add, remove, reorder), and a Player who is building their list would otherwise be blocked until they reached the minimum in one go. Having Start enforce a number that saving ignores would just move the surprise to the Creator. The maximum is still enforced. It counts priority positions, so tournament topics and the empty slots between them both count (`priorityIndex < max`).

## Consequences

- The settings form still validates min ≤ max, so the two numbers stay coherent even though only one of them is enforced.
- A tournament can start with Players who brought fewer topics than the minimum, or none at all.
