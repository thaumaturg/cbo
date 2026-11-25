import domainLimits from "@/generated/domain-limits.json";

/**
 * Domain limits and defaults shared with the server.
 *
 * The values come from the backend's `Limits`, `TournamentDefaults` and `PagingDefaults` constants, through
 * `src/generated/domain-limits.json`, which a backend test regenerates. Change them on the server, never here.
 * See docs/adr/0010-domain-limits-live-in-code-and-ship-to-the-frontend-at-build-time.md.
 */

const deepFreeze = (value) => {
  if (value !== null && typeof value === "object" && !Object.isFrozen(value)) {
    Object.values(value).forEach(deepFreeze);
    Object.freeze(value);
  }
  return value;
};

const { limits, tournamentDefaults, paging } = deepFreeze(domainLimits);

/** Limits, grouped by area as in the backend's `Limits` class. */
export const LIMITS = limits;

/** Settings a new tournament starts with. */
export const TOURNAMENT_DEFAULTS = tournamentDefaults;

/** Paging bounds the server accepts on list endpoints. */
export const PAGING = paging;
