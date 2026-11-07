/**
 * Whether a tournament is still in Preparations. The API only accepts changes to
 * the settings, participants, seeding and topic choices during this stage.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {boolean}
 */
export const isPreparationsStage = (stage) => stage === "Preparations";

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
    default:
      return "success";
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
