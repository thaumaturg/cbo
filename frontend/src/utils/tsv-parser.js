/**
 * TSV (Tab-Separated Values) Parser
 *
 * Handles Google Sheets copy-paste format with proper support for:
 * - Quoted cells containing newlines and tabs
 * - Escaped quotes ("" inside quoted cells)
 * - Smart/curly quotes (" ")
 * - Edge cases where cell content starts with quote characters (like Russian typographic quotes)
 */

/**
 * Check if a character is a quote (straight or curly/smart quotes)
 * @param {string} char - Single character to check
 * @returns {boolean}
 */
const isQuoteChar = (char) => {
  return char === '"' || char === "\u201C" || char === "\u201D"; // " " "
};

/**
 * Parse tab-separated data that may contain quoted cells with newlines.
 * Google Sheets wraps cells containing newlines/tabs in double quotes.
 * Double quotes inside cells are escaped as "".
 *
 * @param {string} text - Raw TSV text from clipboard
 * @returns {string[][]} - 2D array of rows and cells
 *
 * @example
 * // Simple case
 * parseTabSeparatedData("A\tB\nC\tD") // [["A", "B"], ["C", "D"]]
 *
 * @example
 * // Quoted cell with newline
 * parseTabSeparatedData('"Line1\nLine2"\tB') // [["Line1\nLine2", "B"]]
 */
export const parseTabSeparatedData = (text) => {
  const rows = [];
  let currentRow = [];
  let currentCell = "";
  let insideQuotes = false;
  let i = 0;

  while (i < text.length) {
    const char = text[i];
    const nextChar = text[i + 1];
    const charAfterNext = text[i + 2];

    if (insideQuotes) {
      if (isQuoteChar(char) && isQuoteChar(nextChar)) {
        // Escaped quote inside quoted cell ("" or "")
        currentCell += '"';
        i += 2;
      } else if (isQuoteChar(char)) {
        // End of quoted cell
        insideQuotes = false;
        i++;
      } else if (char === "\t" && isQuoteChar(nextChar) && !isQuoteChar(charAfterNext)) {
        // TAB followed by single quote (not escaped "") while "inside quotes"
        // Need to determine if:
        // 1. Real quoted cell with TAB content: "text\t" followed by delimiter
        // 2. False quote situation: "text\t"more content
        const isNextCharClosingQuote =
          charAfterNext === "\t" || charAfterNext === "\n" || charAfterNext === "\r" || charAfterNext === undefined;

        if (isNextCharClosingQuote) {
          // Case 1: The quote IS a real closing quote, TAB is content
          // Add TAB to cell and continue (quote will close on next iteration)
          currentCell += char;
          i++;
        } else {
          // Case 2: False quote - treat TAB as column separator
          currentRow.push(currentCell);
          currentCell = "";
          insideQuotes = false;
          i++;
        }
      } else {
        // Regular character inside quotes (including newlines and tabs)
        currentCell += char;
        i++;
      }
    } else {
      // Check for start of quoted cell - allow leading whitespace
      if (isQuoteChar(char) && currentCell.trim() === "") {
        // Potential start of quoted cell
        // Only treat quote as content if followed by TAB or end (lonely quote between delimiters)
        // Quote followed by newline is legitimate (cell content starts with newline)
        if (nextChar === "\t" || nextChar === undefined) {
          // Lonely quote between delimiters - treat as content, not delimiter
          currentCell += char;
          i++;
        } else {
          // Normal quoted cell start, including cells that begin with newline
          currentCell = "";
          insideQuotes = true;
          i++;
        }
      } else if (char === "\t") {
        // Column separator
        currentRow.push(currentCell);
        currentCell = "";
        i++;
      } else if (char === "\n" || (char === "\r" && nextChar === "\n")) {
        // Row separator
        currentRow.push(currentCell);
        if (currentRow.some((cell) => cell.trim())) {
          rows.push(currentRow);
        }
        currentRow = [];
        currentCell = "";
        i += char === "\r" ? 2 : 1;
      } else if (char === "\r") {
        // Row separator (old Mac style)
        currentRow.push(currentCell);
        if (currentRow.some((cell) => cell.trim())) {
          rows.push(currentRow);
        }
        currentRow = [];
        currentCell = "";
        i++;
      } else {
        currentCell += char;
        i++;
      }
    }
  }

  // Don't forget the last cell/row
  currentRow.push(currentCell);
  if (currentRow.some((cell) => cell.trim())) {
    rows.push(currentRow);
  }

  return rows;
};
