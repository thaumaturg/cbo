/**
 * Clipboard table parser.
 *
 * Spreadsheet apps (Google Sheets, Excel, LibreOffice) put two flavors on the clipboard when cells are copied:
 *
 * - `text/html`  - a real <table> where every cell is an explicit <td> node.
 *   Google Sheets additionally embeds the exact cell value as JSON in a `data-sheets-value` attribute.
 *   This format is unambiguous: spreadsheets never parse the TSV.
 *
 * - `text/plain` - tab-separated text. This format is inherently ambiguous:
 *   a cell whose literal content is `"a\nb"` serializes to the same bytes as a quoted cell containing `a\nb`,
 *   so no TSV parser can always reconstruct the original cells.
 *
 * Therefore we parse the HTML flavor whenever present and fall back to TSV only for plain-text sources.
 *
 * A row is blank when every cell is empty or whitespace. Blank rows are dropped by default.
 * Pass `{ keepBlankRows: true }` to keep them in place, for example where they separate groups of rows.
 */

/**
 * Parse tabular data from a paste event's clipboard.
 *
 * @param {DataTransfer} clipboardData - `event.clipboardData` from a paste event
 * @param {{ keepBlankRows?: boolean }} [options] - `keepBlankRows`: keep blank rows instead of removing them
 * @returns {string[][]} 2D array of rows and cells
 */
export const parseClipboardTable = (clipboardData, { keepBlankRows = false } = {}) => {
  const html = clipboardData.getData("text/html");
  if (html) {
    const rows = parseHtmlTable(html, { keepBlankRows });
    if (rows.some(isNonBlankRow)) return rows;
  }

  const text = clipboardData.getData("text/plain");
  return text ? parseTabSeparatedData(text, { keepBlankRows }) : [];
};

/**
 * Extract cell values from an HTML fragment containing a table.
 *
 * @param {string} html - `text/html` clipboard content
 * @param {{ keepBlankRows?: boolean }} [options] - `keepBlankRows`: keep blank rows instead of removing them
 * @returns {string[][]} 2D array of rows and cells, or [] if no table found
 */
export const parseHtmlTable = (html, { keepBlankRows = false } = {}) => {
  const doc = new DOMParser().parseFromString(html, "text/html");
  const table = doc.querySelector("table");
  if (!table) return [];

  const rows = Array.from(table.querySelectorAll("tr"), (tr) =>
    Array.from(tr.querySelectorAll("td, th"), extractCellText),
  );

  return keepBlankRows ? rows : rows.filter(isNonBlankRow);
};

/**
 * Get the text content of a table cell.
 * Prefers Google Sheets' `data-sheets-value` JSON attribute which carries the exact original value.
 * Otherwise falls back to the rendered text with <br> converted back to newlines.
 *
 * @param {Element} cell - <td> or <th> element
 * @returns {string}
 */
const extractCellText = (cell) => {
  const sheetsValue = cell.getAttribute("data-sheets-value");
  if (sheetsValue) {
    try {
      // Key "2" holds string values, "3" numeric values (Google Sheets schema)
      const parsed = JSON.parse(sheetsValue);
      const value = parsed["2"] ?? parsed["3"];
      if (value !== undefined && value !== null) return String(value);
    } catch {
      // Malformed attribute - fall through to text extraction
    }
  }

  for (const br of cell.querySelectorAll("br")) {
    br.replaceWith("\n");
  }
  return cell.textContent;
};

/**
 * Parse tab-separated text.
 * Cells containing tabs/newlines are expected to be wrapped in double quotes.
 *
 * Sheets does NOT escape quotes inside quoted cells (no "" doubling).
 * So a quote inside a quoted cell is treated as a closing quote only when a delimiter
 * (tab, newline, end of input) follows it. Otherwise it is content.
 * Escaped "" pairs are still honored for Excel compatibility.
 *
 * Fallback for plain-text sources only.
 * Spreadsheet pastes go through parseHtmlTable, which is unambiguous.
 *
 * @param {string} text - Raw TSV text
 * @param {{ keepBlankRows?: boolean }} [options] - `keepBlankRows`: keep blank rows instead of removing them
 * @returns {string[][]} 2D array of rows and cells
 */
export const parseTabSeparatedData = (text, { keepBlankRows = false } = {}) => {
  const rows = [];
  let row = [];
  let cell = "";
  let insideQuotes = false;
  let i = 0;

  const endCell = () => {
    row.push(cell);
    cell = "";
  };
  const endRow = () => {
    endCell();
    rows.push(row);
    row = [];
  };

  while (i < text.length) {
    const char = text[i];

    if (insideQuotes) {
      if (char === '"') {
        const next = text[i + 1];
        if (next === '"') {
          cell += '"'; // Escaped quote (Excel-style)
          i += 2;
        } else if (next === "\t" || next === "\n" || next === "\r" || next === undefined) {
          insideQuotes = false;
          i++;
        } else {
          cell += char; // Unescaped inner quote (Google Sheets-style)
          i++;
        }
      } else {
        cell += char;
        i++;
      }
    } else if (char === '"' && cell === "") {
      insideQuotes = true;
      i++;
    } else if (char === "\t") {
      endCell();
      i++;
    } else if (char === "\n" || char === "\r") {
      endRow();
      i += char === "\r" && text[i + 1] === "\n" ? 2 : 1;
    } else {
      cell += char;
      i++;
    }
  }
  endRow();

  return keepBlankRows ? rows : rows.filter(isNonBlankRow);
};

const isNonBlankRow = (row) => row.some((cell) => cell.trim() !== "");
