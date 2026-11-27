/**
 * Whether a tournament is still in Preparations. The API only accepts changes to
 * the settings, participants, seeding and tournament topics during this stage.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {boolean}
 */
export const isPreparationsStage = (stage) => stage === "Preparations";

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
