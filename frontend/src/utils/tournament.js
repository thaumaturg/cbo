/**
 * Whether a tournament is still in Preparations. The API only accepts changes to
 * the settings, participants, seeding and topic choices during this stage.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {boolean}
 */
export const isPreparationsStage = (stage) => stage === "Preparations";

/**
 * Map a tournament stage to the PrimeVue Badge severity used to display it.
 * @param {string|null|undefined} stage - Tournament stage name
 * @returns {string} - Badge severity
 */
export const stageBadgeSeverity = (stage) => {
  switch (stage) {
    case "Preparations":
      return "secondary";
    case "Qualifications":
      return "info";
    default:
      return "success";
  }
};
