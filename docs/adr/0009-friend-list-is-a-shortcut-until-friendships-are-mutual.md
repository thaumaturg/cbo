# The friend list is a shortcut until friendships are mutual

Users found it awkward to add co-authors and tournament participants by typing exact usernames. We added a friend list that the topic owner and the tournament creator pick from, but for now it's only a shortcut: anyone can still be added by exact username, and nothing checks friendship. The list is one-sided (you add an account without asking it, and it isn't told), so letting only friends be added would protect nobody and only add a step. We plan to make friendships mutual (request, then accept), and only then make the friend list the gate, so that nobody can be made an author or participant without having agreed to be your friend first.

## Considered options

- **Only friends can be added, with a one-sided list**: this sets up the gate early, but until friends have to agree it only adds a step (make someone your friend, then add them) and protects nobody.
- **Mutual friendships from the start**: this needs friend requests, accept and decline, and a way to show incoming requests. None of that exists in the app yet, so we postponed it.

## Consequences

- The pickers keep a way to add anyone by exact username next to the friend list. It goes away when the gate comes.
- When the gate comes, it should check only people being newly added. Existing co-authors and participants who aren't friends would stay, and removing a friend wouldn't remove them from anything. Whether a topic owner who isn't an author may still add anyone needs deciding then, because it would add someone without their agreement.
- `GET /Friends` returns the whole list, not a page, which is an exception to ADR-0008. The pickers need every friend at once, and a friend list is small.
