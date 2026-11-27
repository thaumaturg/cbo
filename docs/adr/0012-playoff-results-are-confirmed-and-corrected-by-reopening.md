# Playoff results are confirmed, and corrected by reopening

A playoff match, shootout or Final doesn't send anyone on when its last round is saved. The Creator has to **Confirm** it. Confirming locks the match and creates whatever its result calls for: the next shootout, or the next matches whose players are now all known. The locked match can't be edited. To correct it, the Creator **Reopens** it, which deletes every match created from its result, and everything created after those, with their rounds. Confirming it again rebuilds them. We did this because rounds are saved one at a time. Creating matches automatically on the last save would turn a typo in that round into a false shootout or the wrong winners, already locked in place. Recalculating later matches when an earlier result changes would mean moving players between matches that may already have rounds played. Deleting and rebuilding is the only correction whose effect is easy to predict and to show in a dialog.

## Considered Options

- **Create the next matches automatically when the last round is saved**: rejected because one wrong answer in the last round, or a round saved by mistake, permanently changes the bracket.
- **Keep every result editable and recalculate later matches**: rejected because a changed winner would have to leave a match that may already have answers recorded for them. There is no correct way to carry those answers over.
- **Lock without any undo**: rejected because a mistake noticed one match later could only be fixed by rolling the whole tournament back to Qualifications.

## Consequences

- How matches connect comes from the bracket template alone, not from stored links. A match stores only its bracket position, or for a shootout the match it settles and its sequence number. Each player stores their Outcome once the match is decided. Reopen deletes the existing matches that come after it in the template, plus its later shootouts.
- Reopen can delete rounds that were already played in later matches. Its confirm dialog lists every match it will delete and how many rounds each has.
- Topics used by deleted rounds count as unplayed again.
- Confirm, and every other command that changes the Playoff or a match's rounds, locks the tournament's row first. Without the lock, two confirmations at once (#1 and #2) would each see the other match as undecided, and neither would create #3, #4 and #5. With it they run one at a time and the next matches are created exactly once. The unique match number per tournament, the same guard Start Qualifications uses, stays as a backstop: a playoff match's number is fixed by its bracket position, so creating it twice collides.
- Qualification matches don't use Confirm. They lock when the Playoff starts and unlock when the tournament is rolled back to Qualifications.
