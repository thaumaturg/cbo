# Only friends can be made authors or participants

Users found it awkward to add co-authors and tournament participants by typing exact usernames. We added a friend list and made it the gate: a topic owner can only add their friends as co-authors, and a tournament creator can only add their friends as participants. For now the list is one-sided: you add an account without asking it, and it isn't told. We plan to make friendships mutual (request, then accept), and the gate is the first step towards that. Once friendships are mutual, the gate means nobody can be made an author or participant without having agreed to be your friend first.

## Considered options

- **The friend list as a shortcut only, with add-by-username still allowed for anyone**: this is the most convenient choice today, but it doesn't lead anywhere. When friendships become mutual, strangers could still be added without consent.
- **Mutual friendships from the start**: this needs friend requests, accept and decline, and a way to show incoming requests. None of that exists in the app yet, so we postponed it.

## Consequences

- With a one-sided list the gate protects nobody yet. It adds a step instead: make someone your friend, then add them. The pickers soften this with an inline "Add a friend" dialog.
- The rule is checked only when someone is newly added. Existing co-authors and participants who aren't friends stay, and removing a friend doesn't remove them from anything.
- **Exception:** when the topic owner has unticked "I am an author", they're entering a topic written by others. In that case they can still add any account as a co-author by exact username. Look at this again when friendships become mutual, because the exception can add someone without their agreement.
- `GET /Friends` returns the whole list, not a page, which is an exception to ADR-0008. The pickers need every friend at once, and a friend list is small.
