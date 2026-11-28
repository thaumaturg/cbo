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
 * Whether the Final exists and every player in it has a place.
 * @param {Array<Object>} matches - The tournament's matches from the API
 * @returns {boolean}
 */
export const isFinalDecided = (matches) =>
  matches.some((match) => match.type === "Final" && match.matchParticipants.every((player) => player.outcome != null));

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
 * @param {Object} [settledMatch] - For a shootout, the playoff match or Final it settles
 * @returns {string}
 */
export const matchTitle = (match, settledMatch) => {
  switch (match.type) {
    case "Playoff":
    case "Final":
      return bracketPositionTitle(match.bracketPosition);
    case "Shootout":
      return `${matchTitle(settledMatch)} · Shootout ${match.shootoutNumber}`;
    default:
      return `${match.createdOnStage} #${match.numberInStage}`;
  }
};

const OUTCOME_LABELS = {
  FirstWinner: "1st winner",
  SecondWinner: "2nd winner",
  Loser: "Loser",
  FirstPlace: "1st place",
  SecondPlace: "2nd place",
  ThirdPlace: "3rd place",
  FourthPlace: "4th place",
};

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
