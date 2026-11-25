# Competitive Bracket Organizer

Organizes tournaments in which participants bring topics and players compete in matches of rounds.

## Language

### Accounts

**Account**:
A person's registration in the organizer, identified by a unique username and a unique email. One account can take part in many tournaments.
_Avoid_: User, profile, login (as a noun for the account)

**Friend**:
An account that another account has put on its friend list, so it can quickly pick them as an author or participant. For now the list is one-sided: the friend isn't asked and doesn't know.
_Avoid_: Contact, buddy, follower, connection

### Settings and limits

**Tournament settings**:
The numbers a Creator picks for one tournament: its tournament size, the minimum number of topics each participant should bring, and the maximum they may bring. The minimum is only a recommendation; the maximum is enforced. A new tournament starts from the defaults, and the Creator can change them.
_Avoid_: Default settings (for the whole set), config

**Default**:
The value a tournament setting has until the Creator changes it.

**Limit**:
A rule the organizer applies to everyone in every tournament, which no Creator can change, such as the highest question cost or the number of rounds in a match.
_Avoid_: Default, setting, config

**Tournament size**:
How many players a tournament has. It can only be one of the sizes the organizer supports, because each size has its own fixed qualification layout.
_Avoid_: Players per tournament, player count

### Tournament lifecycle

**Stage**:
A phase a tournament passes through: Preparations, then Qualifications, then Playoff.
_Avoid_: Phase, step

**Start**:
The move of a tournament from the Preparations stage to the Qualifications stage, which generates its qualification matches.
_Avoid_: Advance stage, advance to Qualifications, play

**Started**:
Said of a tournament once it has left the Preparations stage, whichever stage it is in now.
_Avoid_: In progress, active, running

### Matches

**Match**:
A game in which a few of a tournament's players compete over a fixed number of rounds. How many rounds depends on the kind of match: a qualification match has 4, a final has 6.
_Avoid_: Game, heat, bout

**Round**:
One part of a match, in which exactly one topic is played.
_Avoid_: Turn

### Topics

**Topic**:
A titled set of questions, each with a cost and an answer, that can be brought to many tournaments.
_Avoid_: Theme, question set

**Owner**:
The one account that manages a topic: edits it, deletes it and decides its authors.
_Avoid_: Creator (that is a tournament role)

**Author**:
An account credited with writing a topic's questions. The owner is an author only if they say so.
_Avoid_: Co-owner, contributor

**Co-author**:
An author of a topic who isn't its owner. The owner decides who the co-authors are.
_Avoid_: Contributor

**Played**:
Said of a topic once a round has used it; a played topic can no longer be changed or deleted.

**Import**:
Creating many topics at once from rows copied out of a spreadsheet.
_Avoid_: Upload, bulk create

**Bring**:
What a participant does when they add one of their topics to a tournament.
_Avoid_: Assign, choose

**Tournament topic**:
A topic one participant brings to one tournament, at a priority. The same topic can be a tournament topic in many tournaments.
_Avoid_: Assigned topic, chosen topic, topic assignment

**Empty slot**:
A skipped priority position among a participant's tournament topics; the tournament topics after it keep their lower priority.
_Avoid_: Placeholder
