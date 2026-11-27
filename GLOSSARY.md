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
The choices a Creator makes for one tournament: its **Format**, the minimum number of topics each participant should bring, and the maximum they may bring. The minimum is only a recommendation; the maximum is enforced. A new tournament starts from the defaults, and the Creator can change them.
_Avoid_: Default settings (for the whole set), config

**Default**:
The value a tournament setting has until the Creator changes it.

**Limit**:
A rule the organizer applies to everyone in every tournament, which no Creator can change, such as the highest question cost or the number of rounds in a match.
_Avoid_: Default, setting, config

**Format**:
One of the fixed shapes a tournament can take, chosen by the Creator: a **Tournament size** together with its qualification layout and, where one exists, its **Playoff** bracket. One size can have several formats, such as a short and a long one.
_Avoid_: Size (for the whole shape), mode, type

**Tournament size**:
How many players a tournament has, given by its **Format**.
_Avoid_: Players per tournament, player count

### Tournament lifecycle

**Stage**:
A phase a tournament passes through: Preparations, then Qualifications, then Playoff, then Ended.
_Avoid_: Phase, step

**Playoff**:
The stage after Qualifications, in which players are placed into a **Winners bracket** by their **Qualification place** and play down to a **Final**.
_Avoid_: Playoffs, knockout

**Start Qualifications**:
The move of a tournament from the Preparations stage to the Qualifications stage, which generates its qualification matches.
_Avoid_: Start (alone), advance stage, play

**Start Playoff**:
The move of a tournament from the Qualifications stage to the **Playoff** stage, once every qualification match is finished.
_Avoid_: Start (alone), advance stage

**Ended**:
The last stage, in which nothing in the tournament can change unless it is **Rolled back**.
_Avoid_: Finished, completed, closed

**End Tournament**:
The move of a tournament from the **Playoff** stage to the **Ended** stage, once its **Final** is **Decided**.
_Avoid_: Finish, close, complete

**Roll back**:
The Creator's move of a tournament to an earlier stage, which keeps that stage's results and wipes everything played after it.
_Avoid_: Reset, revert, restart

**Started**:
Said of a tournament once it has left the Preparations stage, whichever stage it is in now.
_Avoid_: In progress, active, running

### Matches

**Match**:
A game in which a few of a tournament's players compete over a fixed number of rounds. How many rounds depends on the kind of match: a qualification or playoff match has 4, a **Final** has 6, a **Shootout** has 1.
_Avoid_: Game, heat, bout

**Round**:
One part of a match, in which exactly one topic is played.
_Avoid_: Turn

**Score**:
The sum of question costs a player wins and loses over a round or a match. Summed over all of a player's qualification matches it is their **Qualification score**.
_Avoid_: Points (that is the place-based reward), result

**Positive score**:
The part of a **Score** a player won, ignoring what they lost.
_Avoid_: Plus score, gained score

**Points**:
What a player earns from a finished qualification match for where they finish in it by **Score**; tied players share the points of the places they cover.
_Avoid_: Score

**Seed**:
A player's order in a tournament, set by the Creator during Preparations, which decides their place in the qualification layout.
_Avoid_: Qualification place, rank

**Qualification place**:
A player's rank by their qualification results, which decides where they enter the **Playoff**; it becomes fixed when the Playoff starts.
_Avoid_: Seed, standing, rank

### Playoff

**Winners bracket**:
The part of the **Playoff** a player stays in until they lose a match once.
_Avoid_: Upper bracket, main bracket

**Losers bracket**:
The part of the **Playoff** a player drops into after losing a match in the **Winners bracket**; losing here eliminates them.
_Avoid_: Lower bracket, consolation bracket

**Playoff match**:
One of the numbered four-player matches of a **Playoff** bracket that send two winners on and two losers down or out; neither the **Final** nor a **Shootout** is one.
_Avoid_: Bracket match, knockout match

**Advance**:
To finish a playoff match as one of its two winners and move to the next match in the same bracket, or into the **Final**.
_Avoid_: Promote, qualify

**Drop**:
To lose a match in the **Winners bracket** and move into the **Losers bracket**.
_Avoid_: Fall, demote

**Eliminated**:
Said of a player who has lost a match in the **Losers bracket** and is out of the tournament.
_Avoid_: Knocked out, kicked out

**Final**:
The last match of a tournament, between the two winners of each bracket's last match, which decides every place from first to fourth.
_Avoid_: Grand final

**Champion**:
The player who finishes first in the **Final**.
_Avoid_: Winner (alone)

**Tie**:
The outcome of a **Playoff match** when its second and third best **Scores** are equal, so its two winners can't be told apart from its losers; in the **Final**, any equal **Scores**.
_Avoid_: Draw

**Shootout**:
A one-round match that belongs to a **Playoff match** or the **Final**, played by the players of a **Tie** to settle it; repeated with whoever is still tied until it is settled.
_Avoid_: Tiebreaker match, overtime

**Decided**:
Said of a **Playoff match** or the **Final** once it and its **Shootouts**, if any, are **Confirmed** and no further Shootout is needed; every player in it then has an **Outcome**.
_Avoid_: Finished (that is only about rounds), done

**Outcome**:
What a player got from a **Decided** match: 1st winner, 2nd winner or loser in a **Playoff match**; their place, first to fourth, in the **Final**.
_Avoid_: Result, finish

**Locked**:
Said of a match whose rounds can no longer be added, changed or deleted: a **Playoff match**, **Shootout** or **Final** once **Confirmed**, a qualification match once the **Playoff** has started.
_Avoid_: Closed, frozen

**Confirm**:
The Creator's act of accepting the result of a finished **Playoff match**, **Shootout** or **Final**, which locks it and lets the tournament go on from it: to a next **Shootout**, or to the next matches.
_Avoid_: Submit, approve, close

**Reopen**:
The Creator's act of undoing a **Confirm**: it deletes every match created from the confirmed match's result, and everything created after those, then unlocks it.
_Avoid_: Unconfirm, undo, unlock (alone)

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
Said of a topic while a round uses it; a played topic can't be changed or deleted.

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
