/**
 * Whether a tournament is still in Preparations. The API only accepts changes to
 * the settings, participants, seeding and tournament topics during this stage.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {boolean}
 */
export const isPreparationsStage = (stage) => stage === "Preparations";

/**
 * Whether a tournament's Playoff has started: it is in the Playoff or has ended.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {boolean}
 */
export const hasPlayoffStarted = (stage) => stage === "Playoff" || stage === "Ended";

/** The tournament stages in the order a tournament moves through them. */
export const TOURNAMENT_STAGES = ["Preparations", "Qualifications", "Playoff", "Ended"];

const ROLL_BACK_WIPES = {
  Preparations:
    "Every match, round and answer, all totals, and the start and end times will be deleted. Participants, seeding, topics and settings can be edited again.",
  Qualifications:
    "Every playoff match, shootout and the Final will be deleted with their rounds, the qualification places will no longer be fixed, and the end time will be cleared. Qualification matches can be edited again.",
  Playoff: "The end time will be cleared. Confirmed matches stay confirmed; reopen a match to correct it.",
};

/**
 * What rolling a tournament back to a stage wipes, for the confirm dialog.
 * @param {string} stage - The earlier stage the tournament goes back to
 * @returns {string}
 */
export const rollBackWipes = (stage) => ROLL_BACK_WIPES[stage] ?? "";

const FORMAT_LABELS = {
  TwelveShort: "12 players · Short",
  TwelveLong: "12 players · Long",
  Fourteen: "14 players",
};

/**
 * The label shown for a tournament format.
 * @param {string|null|undefined} format - Tournament format name, as the API sends it
 * @returns {string}
 */
export const formatLabel = (format) => FORMAT_LABELS[format] ?? format ?? "";

/**
 * Map a tournament stage to the PrimeVue Tag severity used to display it.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {string} - Tag severity
 */
export const stageTagSeverity = (stage) => {
  switch (stage) {
    case "Preparations":
      return "secondary";
    case "Qualifications":
      return "info";
    case "Playoff":
      return "warn";
    case "Ended":
      return "success";
    default:
      return "secondary";
  }
};

/**
 * Map a participant role to the PrimeVue Tag severity used to display it.
 * @param {string|null|undefined} role - Participant role name
 * @returns {string} - Tag severity
 */
export const roleTagSeverity = (role) => {
  switch (role) {
    case "Creator":
      return "contrast";
    case "Organizer":
      return "info";
    case "Player":
      return "success";
    default:
      return "secondary";
  }
};
