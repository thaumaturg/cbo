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

// "Playoff3" -> 3, "Final" -> 10, as the API numbers bracket positions
const bracketNumber = (bracketPosition) =>
  bracketPosition === "Final" ? 10 : Number(bracketPosition?.replace("Playoff", ""));

/**
 * A match's name: "Qualifications #4", "Playoff #3", "Final", "Playoff #3 · Shootout 1" or "Final · Shootout 2".
 * @param {Object} match - A match from the API
 * @param {Object} [settledMatch] - For a shootout, the playoff match or Final it settles
 * @returns {string}
 */
export const matchTitle = (match, settledMatch) => {
  switch (match.type) {
    case "Playoff":
      return `Playoff #${bracketNumber(match.bracketPosition)}`;
    case "Final":
      return "Final";
    case "Shootout":
      return `${matchTitle(settledMatch)} · Shootout ${match.shootoutNumber}`;
    default:
      return `${match.createdOnStage} #${match.numberInStage}`;
  }
};

/**
 * Playoff matches, shootouts and the Final in bracket order, each match's shootouts right after it.
 * @param {Array<Object>} matches - The tournament's matches from the API
 * @returns {Array<{ match: Object, title: string }>}
 */
export const playoffMatchesInBracketOrder = (matches) => {
  const byId = new Map(matches.map((match) => [match.id, match]));

  return matches
    .filter((match) => !isQualificationMatch(match))
    .map((match) => {
      const settledMatch = byId.get(match.settledMatchId);
      const position = bracketNumber((settledMatch ?? match).bracketPosition);
      return { match, title: matchTitle(match, settledMatch), position, shootout: match.shootoutNumber ?? 0 };
    })
    .sort((a, b) => a.position - b.position || a.shootout - b.shootout)
    .map(({ match, title }) => ({ match, title }));
};
