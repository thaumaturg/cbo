import { LIMITS } from "@/domain/limits.js";

/**
 * Whether a match has all the rounds its kind plays.
 * @param {Object} match - A match from the API, with `type` and `roundsCount`
 * @returns {boolean}
 */
export const isMatchFinished = (match) => match.roundsCount >= (LIMITS.matches.roundsPerMatch[match.type] ?? Infinity);

/**
 * Whether a match is a qualification match, as opposed to a playoff match, shootout or the Final.
 * @param {Object} match - A match from the API
 * @returns {boolean}
 */
export const isQualificationMatch = (match) => match.type === "Qualification";

/**
 * Whether a playoff match or the Final is decided: every player in it has an Outcome.
 * @param {Object} match - A match from the API
 * @returns {boolean}
 */
export const isMatchDecided = (match) =>
  match.matchParticipants.length > 0 && match.matchParticipants.every((player) => player.outcome != null);

/**
 * Whether the Final exists and every player in it has a place.
 * @param {Array<Object>} matches - The tournament's matches from the API
 * @returns {boolean}
 */
export const isFinalDecided = (matches) => matches.some((match) => match.type === "Final" && isMatchDecided(match));

/**
 * What confirming a playoff match, shootout or Final led to, read from the tournament's matches.
 * @param {Object} match - A match from the API
 * @param {Array<Object>} matches - The tournament's matches from the API
 * @returns {{ settledMatch: Object|null, shootout: Object|null, decidedMatch: Object|null }|null} The match being
 *   settled (the match itself or, for a shootout, the match it settles); the shootout its confirmation created; and the
 *   settled match once decided, by this confirmation or, for a playoff match or Final, by its shootouts. Null while
 *   unconfirmed.
 */
export const confirmationFollowUp = (match, matches) => {
  if (!match.confirmedAt) return null;

  const settledMatchId = match.type === "Shootout" ? match.settledMatchId : match.id;
  const settledMatch = matches.find((other) => other.id === settledMatchId) ?? null;
  const nextShootoutNumber = (match.shootoutNumber ?? 0) + 1;
  const shootout =
    matches.find(
      (other) =>
        other.type === "Shootout" &&
        other.settledMatchId === settledMatchId &&
        other.shootoutNumber === nextShootoutNumber,
    ) ?? null;
  // A shootout followed by another didn't decide the match; the last one did
  const decided = settledMatch && isMatchDecided(settledMatch) && (settledMatch.id === match.id || !shootout);

  return { settledMatch, shootout, decidedMatch: decided ? settledMatch : null };
};

/**
 * The name of a bracket position: "Playoff #3" or "Final".
 * @param {string} bracketPosition - A bracket position as the API names it, such as "Playoff3" or "Final"
 * @returns {string}
 */
export const bracketPositionTitle = (bracketPosition) =>
  bracketPosition === "Final" ? "Final" : bracketPosition.replace("Playoff", "Playoff #");

/**
 * A match's name: "Qualifications #4", "Playoff #3", "Final", "Playoff #3 · Shootout 1" or "Final · Shootout 2".
 * @param {Object} match - A match from the API
 * @param {Object} [settledMatch] - For a shootout, the playoff match or Final it settles, or anything with its `bracketPosition`
 * @returns {string}
 */
export const matchTitle = (match, settledMatch) => {
  switch (match.type) {
    case "Playoff":
    case "Final":
      return bracketPositionTitle(match.bracketPosition);
    case "Shootout":
      return settledMatch
        ? `${bracketPositionTitle(settledMatch.bracketPosition)} · Shootout ${match.shootoutNumber}`
        : `Shootout ${match.shootoutNumber}`;
    default:
      return `${match.createdOnStage} #${match.numberInStage}`;
  }
};

/**
 * The name of a match that reopening another would delete: "Playoff #5", "Final" or "Playoff #3 · Shootout 2".
 * @param {Object} deletion - A reopen deletion from the API
 * @returns {string}
 */
export const reopenDeletionTitle = (deletion) =>
  matchTitle(deletion, { bracketPosition: deletion.settledBracketPosition });

const OUTCOME_LABELS = {
  FirstWinner: "1st winner",
  SecondWinner: "2nd winner",
  Loser: "Loser",
  FirstPlace: "1st place",
  SecondPlace: "2nd place",
  ThirdPlace: "3rd place",
  FourthPlace: "4th place",
};

const OUTCOME_ORDER = Object.keys(OUTCOME_LABELS);

/**
 * A decided match's players by Outcome: winners in order then losers, or places 1 to 4.
 * @param {Object} match - A match from the API
 * @returns {Array<Object>}
 */
export const playersByOutcome = (match) =>
  [...match.matchParticipants].sort((a, b) => OUTCOME_ORDER.indexOf(a.outcome) - OUTCOME_ORDER.indexOf(b.outcome));

/**
 * What a player got from a decided playoff match or Final: "1st winner", "Loser", "Eliminated", "2nd place" and so on.
 * @param {Object} player - A match participant from the API, with `outcome` and `isEliminated`
 * @returns {string|null} Null until the match is decided
 */
export const outcomeLabel = (player) => (player.isEliminated ? "Eliminated" : (OUTCOME_LABELS[player.outcome] ?? null));

const OUTCOME_TAG_SEVERITIES = {
  FirstWinner: "success",
  SecondWinner: "success",
  Loser: "warn",
  FirstPlace: "success",
  SecondPlace: "info",
  ThirdPlace: "info",
  FourthPlace: "secondary",
};

/**
 * PrimeVue Tag severity for a player's outcome, matching {@link outcomeLabel}.
 * @param {Object} player - A match participant from the API, with `outcome` and `isEliminated`
 * @returns {string|undefined}
 */
export const outcomeTagSeverity = (player) => (player.isEliminated ? "danger" : OUTCOME_TAG_SEVERITIES[player.outcome]);
