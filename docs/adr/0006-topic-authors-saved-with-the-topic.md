# Topic authors are saved with the topic

A topic's co-authors used to have their own endpoints (`Topics/{topicId}/authors`), which saved each add or remove at once, while the topic's details and questions waited for Save. On the topic page the two sat side by side, so one Save button covered only part of what the user had edited. We dropped the author endpoints. The create-topic and update-topic requests now carry the full list of co-author usernames, and the command handler syncs it in the same transaction as the details and questions (see ADR-0003): the whole topic is saved, or none of it. Every author problem comes back as a field error on its row (`authors[i].username`), the same way question problems do.

## Considered options

- **Keep the author endpoints and have the frontend call them after the topic save**: needs no backend change, but it isn't atomic. The topic can save while an author add fails, and the user is left with a half-saved topic and a confusing error.
- **Keep the author endpoints alongside the batched save**: leaves two ways to change authors, with two sets of rules to keep consistent.

## Consequences

- `TopicResponse.Authors` is the only way to read authors; there is no author resource anymore.
- The owner is never part of the request's author list. The owner's "I am an author" flag still travels as `isAuthor`.
- A played topic can't be edited at all, so its authors can't change either. No separate rule is needed.
