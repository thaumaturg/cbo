# Format replaces tournament size as the setting

The Creator no longer picks a tournament size. They pick a **Format**, such as "12 players · Short" or "12 players · Long". The tournament size, the qualification layout and the playoff bracket all follow from the Format. We did this because 12 players can be played with two qualification layouts of different lengths, so a number can no longer identify a layout. The playoff bracket also exists only for some formats: 14 has a qualification layout but no bracket yet. We chose one setting over two (size plus qualification length) because only some combinations exist. Two settings would let the Creator pick pairs that need extra validation and an explanation, while one select shows only the formats that work.

## Considered Options

- **Size plus a separate qualification-length setting**: rejected because it invites invalid pairs and needs a rule for which sizes support which lengths.
- **Size options like "12-short" and "12-long"**: rejected because they make "size" mean something other than how many players there are, which the glossary and code rely on.

## Consequences

- Qualification tables and the bracket are keyed by Format, not by size. Code that needs the number of players reads it from the Format.
- Existing tournaments are migrated to Format 14.
- A Format can exist in code but not be selectable. Format 14 stays that way until its bracket is designed, and creating or updating a tournament with it is rejected.
- Selectable formats and the default ship to the frontend with the other limits (ADR 0010).
