import domainLimits from "@/generated/domain-limits.json";

/**
 * Domain limits and defaults shared with the server.
 *
 * The values come from the backend's `Limits`, `TournamentDefaults` and `PagingDefaults` constants and its playoff
 * bracket templates, through `src/generated/domain-limits.json`, which a backend test regenerates. Change them on the
 * server, never here.
 * See docs/adr/0010-domain-limits-live-in-code-and-ship-to-the-frontend-at-build-time.md.
 */

const deepFreeze = (value) => {
  if (value !== null && typeof value === "object" && !Object.isFrozen(value)) {
    Object.values(value).forEach(deepFreeze);
    Object.freeze(value);
  }
  return value;
};

const { limits, tournamentDefaults, paging, playoffBrackets } = deepFreeze(domainLimits);

/** Limits, grouped by area as in the backend's `Limits` class. */
export const LIMITS = limits;

/** Settings a new tournament starts with. */
export const TOURNAMENT_DEFAULTS = tournamentDefaults;

/** Paging bounds the server accepts on list endpoints. */
export const PAGING = paging;

/**
 * Each format's playoff bracket template, by format and then bracket position: the seats of every playoff match and
 * the Final, and where its winners and losers go. Formats without a bracket are missing.
 */
export const PLAYOFF_BRACKETS = playoffBrackets;
