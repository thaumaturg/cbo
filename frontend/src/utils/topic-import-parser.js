import { LIMITS } from "@/domain/limits.js";

/**
 * Topic import parser.
 *
 * Reads topics out of rows pasted from the topics sheet. Each topic is a block of rows, with a blank row between blocks:
 *
 * - row 1, the title row: A = title
 * - row 2, the header row: A = description (or the `Комментарий к теме:` placeholder), B–D = labels, ignored
 * - rows 3 and on, the question rows: A = cost, B = question, C = answer, D = comment
 *
 * See docs/specs/topic-import.md, "Parsing rows into topics".
 */

const COLUMN_COUNT = 4;
const DESCRIPTION_PLACEHOLDER = "Комментарий к теме:";
const WHOLE_NUMBER = /^\d+$/;

// Position of the first question row inside its block; the title row is row 1
const FIRST_QUESTION_ROW_NUMBER = 3;

/**
 * Split pasted rows into topics.
 * A block with no question rows is an empty topic: it's left out of `topics` and counted in `skippedCount`.
 *
 * @param {string[][]} rows - Rows and cells, with blank rows kept (`parseClipboardTable(..., { keepBlankRows: true })`)
 * @returns {{
 *   topics: {
 *     title: string,
 *     description: string | null,
 *     questions: { questionNumber: number, cost: number | null, text: string, answer: string, comment: string | null }[],
 *     totalCost: number,
 *     errors: string[],
 *   }[],
 *   skippedCount: number,
 * }} The topics in sheet order; a topic is valid when `errors` is empty. `cost` is null when it isn't a whole number.
 */
export const parseTopicBlocks = (rows) => {
  const topics = [];
  let skippedCount = 0;

  for (const block of splitIntoBlocks(rows.map(toCells))) {
    const topic = parseBlock(block);
    if (topic) {
      topics.push(topic);
    } else {
      skippedCount++;
    }
  }

  return { topics, skippedCount };
};

// Columns A–D, trimmed; missing cells are empty and columns past D are dropped
const toCells = (row) => Array.from({ length: COLUMN_COUNT }, (_, index) => (row[index] ?? "").trim());

const isBlank = (cells) => cells.every((cell) => cell === "");

const splitIntoBlocks = (rows) => {
  const blocks = [];
  let block = [];

  for (const cells of rows) {
    if (!isBlank(cells)) {
      block.push(cells);
    } else if (block.length > 0) {
      blocks.push(block);
      block = [];
    }
  }
  if (block.length > 0) blocks.push(block);

  return blocks;
};

// The topic in a block, or null when it has no question rows
const parseBlock = ([titleRow, headerRow, ...questionRows]) => {
  const filledRows = questionRows
    .map((cells, index) => toQuestionRow(cells, FIRST_QUESTION_ROW_NUMBER + index))
    .filter(({ text, answer }) => text !== "" || answer !== "");
  if (filledRows.length === 0) return null;

  const title = titleRow[0];
  const questions = filledRows.map(({ cost, text, answer, comment }, index) => ({
    questionNumber: index + 1,
    cost,
    text,
    answer,
    comment,
  }));
  const totalCost = questions.reduce((sum, question) => sum + (question.cost ?? 0), 0);
  const errors = [
    ...titleErrors(title),
    ...filledRows.flatMap(questionRowErrors),
    ...topicErrors(questions.length, totalCost),
  ];

  return { title, description: parseDescription(headerRow[0]), questions, totalCost, errors };
};

const toQuestionRow = ([cost, text, answer, comment], rowNumber) => ({
  rowNumber,
  cost: WHOLE_NUMBER.test(cost) ? Number(cost) : null,
  text,
  answer,
  comment: comment || null,
});

const parseDescription = (cell) => {
  const description = cell.startsWith(DESCRIPTION_PLACEHOLDER)
    ? cell.slice(DESCRIPTION_PLACEHOLDER.length).trim()
    : cell;
  return description || null;
};

const titleErrors = (title) => {
  if (title === "") return ["Title is missing."];
  if (title.length > LIMITS.topics.titleLengthMax) {
    return [`Title can be at most ${LIMITS.topics.titleLengthMax} characters (currently ${title.length}).`];
  }
  return [];
};

const questionRowErrors = ({ rowNumber, cost, text, answer }) => {
  const { questionCostMin, questionCostMax } = LIMITS.topics;
  const errors = [];

  if (text === "" || answer === "") {
    errors.push(`Row ${rowNumber}: question and answer are both required.`);
  }
  if (cost === null || cost < questionCostMin || cost > questionCostMax) {
    errors.push(`Row ${rowNumber}: cost must be a whole number from ${questionCostMin} to ${questionCostMax}.`);
  }

  return errors;
};

const topicErrors = (questionCount, totalCost) => {
  const { questionsMax, questionsCostTotalMax } = LIMITS.topics;
  const errors = [];

  if (questionCount > questionsMax) {
    errors.push(`A topic can have at most ${questionsMax} questions (currently ${questionCount}).`);
  }
  if (totalCost > questionsCostTotalMax) {
    errors.push(`The total cost can be at most ${questionsCostTotalMax} (currently ${totalCost}).`);
  }

  return errors;
};
