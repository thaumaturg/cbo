import { PLAYOFF_BRACKETS } from "@/domain/limits.js";
import { bracketPositionTitle, matchTitle } from "@/utils/match.js";

// How the 12-player bracket is drawn: each chart's last match on top, the matches feeding it below. This is a drawing
// choice, not the template's connections: #1 and #2 both feed #3 and #4, and #8 feeds #9 across charts, so a tree
// can't be derived from the template's sources.
const CHARTS = [
  {
    key: "winners",
    title: "Winners bracket",
    layout: {
      position: "Playoff8",
      feeders: [
        { position: "Playoff3", feeders: [{ position: "Playoff1" }] },
        { position: "Playoff4", feeders: [{ position: "Playoff2" }] },
      ],
    },
  },
  {
    key: "losers",
    title: "Losers bracket",
    layout: {
      position: "Playoff9",
      feeders: [{ position: "Playoff7", feeders: [{ position: "Playoff5" }, { position: "Playoff6" }] }],
    },
  },
  { key: "final", title: "Final", layout: { position: "Final" } },
];

/**
 * The Playoff's bracket charts (winners bracket, losers bracket and Final) as OrganizationChart trees.
 *
 * A match that exists is a node with `data.match`; one that doesn't exist yet is a placeholder with `data.slotLabels`, the
 * template's labels for its players. A match's shootouts sit above it in play order, the last one linking to the
 * match it feeds.
 * @param {string} format - The tournament's format
 * @param {Array<Object>} matches - The tournament's matches from the API
 * @returns {Array<{ key: string, title: string, root: Object }>} Empty when the format has no bracket
 */
export const playoffBracketCharts = (format, matches) => {
  const bracket = PLAYOFF_BRACKETS[format];
  if (!bracket) return [];

  const matchesByPosition = new Map();
  const shootoutsBySettledMatchId = new Map();
  for (const match of matches) {
    if (match.type === "Shootout") {
      if (!shootoutsBySettledMatchId.has(match.settledMatchId)) shootoutsBySettledMatchId.set(match.settledMatchId, []);
      shootoutsBySettledMatchId.get(match.settledMatchId).push(match);
    } else if (match.bracketPosition) {
      matchesByPosition.set(match.bracketPosition, match);
    }
  }

  const buildNode = ({ position, feeders = [] }) => {
    const children = feeders.map(buildNode);
    const match = matchesByPosition.get(position);

    if (!match) {
      const slotLabels = bracket[position].slots.map((slot) => slot.label);
      return { key: position, data: { title: bracketPositionTitle(position), slotLabels }, children };
    }

    const shootouts = (shootoutsBySettledMatchId.get(match.id) ?? []).sort(
      (a, b) => a.shootoutNumber - b.shootoutNumber,
    );
    return shootouts.reduce(
      (node, shootout) => ({
        key: shootout.id,
        data: { title: matchTitle(shootout, match), match: shootout },
        children: [node],
      }),
      { key: match.id, data: { title: matchTitle(match), match }, children },
    );
  };

  return CHARTS.map(({ key, title, layout }) => ({ key, title, root: buildNode(layout) }));
};
