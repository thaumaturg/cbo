import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";

/** Page sizes the list paginators offer. The server accepts 1–100. */
export const PAGE_SIZES = [10, 25, 50];

export const DEFAULT_PAGE_SIZE = 25;

// The server binds `page` to an int, so a larger number would be a binding 400.
const MAX_PAGE = 2 ** 31 - 1;

const firstValue = (value) => (Array.isArray(value) ? value[0] : value);

const toArray = (value) => {
  if (value == null) {
    return [];
  }
  return Array.isArray(value) ? value : [value];
};

const isEmpty = (value) =>
  value == null || (typeof value === "string" && value.trim() === "") || (Array.isArray(value) && value.length === 0);

const isSameValue = (a, b) => {
  if (Array.isArray(a) || Array.isArray(b)) {
    const left = toArray(a).toSorted();
    const right = toArray(b).toSorted();
    return left.length === right.length && left.every((item, index) => item === right[index]);
  }
  return a === b;
};

/**
 * Turn typed list params into a route query. Leaves out empty values and values equal to their default,
 * so the URL only holds what differs from the default. Arrays stay arrays, which the router writes as
 * repeated keys (`role=Creator&role=Player`).
 * @param {Object} params - Typed list params
 * @param {Object} defaults - Default value of each key
 * @returns {Object} - Route query with string values
 */
const toQuery = (params, defaults) => {
  const query = {};
  for (const [key, value] of Object.entries(params)) {
    if (isEmpty(value) || isSameValue(value, defaults[key])) {
      continue;
    }
    query[key] = Array.isArray(value) ? value.map(String) : String(value);
  }
  return query;
};

/**
 * Parse `page` from the route query
 * @param {string | string[] | undefined} value - Raw query value
 * @returns {number} - A whole number ≥ 1, else 1
 */
export function parsePage(value) {
  const text = firstValue(value);
  if (typeof text !== "string" || !/^[1-9]\d*$/.test(text)) {
    return 1;
  }
  const page = Number(text);
  return page <= MAX_PAGE ? page : 1;
}

/**
 * Parse `pageSize` from the route query
 * @param {string | string[] | undefined} value - Raw query value
 * @returns {number} - One of {@link PAGE_SIZES}, else {@link DEFAULT_PAGE_SIZE}
 */
export function parsePageSize(value) {
  const text = firstValue(value);
  const pageSize = typeof text === "string" && /^\d+$/.test(text) ? Number(text) : NaN;
  return PAGE_SIZES.includes(pageSize) ? pageSize : DEFAULT_PAGE_SIZE;
}

/**
 * Parse `sort` from the route query: one of the list's sort keys, with an optional leading `-` for descending
 * @param {string | string[] | undefined} value - Raw query value
 * @param {string[]} keys - The list's sort keys, as the server names them (e.g. `["title", "createdAt"]`)
 * @param {string} fallback - The list's default sort (e.g. `"-createdAt"`)
 * @returns {string} - The sort, else `fallback`
 */
export function parseSort(value, keys, fallback) {
  const text = firstValue(value);
  if (typeof text !== "string") {
    return fallback;
  }
  const key = text.startsWith("-") ? text.slice(1) : text;
  return keys.includes(key) ? text : fallback;
}

/**
 * Parse free text, such as `search`, from the route query
 * @param {string | string[] | undefined} value - Raw query value
 * @returns {string | undefined} - The text as written, or `undefined` when missing or blank
 */
export function parseText(value) {
  const text = firstValue(value);
  return typeof text === "string" && text.trim() !== "" ? text : undefined;
}

/**
 * Parse a single-value enum filter from the route query
 * @param {string | string[] | undefined} value - Raw query value
 * @param {string[]} allowed - Known values
 * @returns {string | undefined} - A known value, else `undefined` (no filter)
 */
export function parseOneOf(value, allowed) {
  const text = firstValue(value);
  return allowed.includes(text) ? text : undefined;
}

/**
 * Parse a repeated enum filter from the route query (`role=Creator&role=Player`)
 * @param {string | string[] | undefined} value - Raw query value
 * @param {string[]} allowed - Known values
 * @returns {string[]} - The known values, without repeats; empty means no filter
 */
export function parseManyOf(value, allowed) {
  return [...new Set(toArray(value).filter((item) => allowed.includes(item)))];
}

/**
 * Parse a yes/no filter from the route query
 * @param {string | string[] | undefined} value - Raw query value
 * @returns {boolean | undefined} - `true` for `"true"`, `false` for `"false"`, else `undefined` (no filter)
 */
export function parseBoolean(value) {
  const text = firstValue(value);
  if (text === "true") {
    return true;
  }
  if (text === "false") {
    return false;
  }
  return undefined;
}

/**
 * Keep a list's paging, sorting and filters in the route query, so reload, Back/Forward and bookmarks
 * restore them. The URL is the only source of truth: read the state from `params` and change it only
 * with `update` or `reset`, from user events. A watcher on `params` may sync a local input (such as the
 * search box text) but must never write to the URL, or the two loop.
 *
 * The keys and values of `params` are the API's, so `params.value` can go straight to the list's service.
 * Call it from the `setup` of a component rendered by a route.
 *
 * @example
 * const SORT_KEYS = ["title", "questionCount", "createdAt"];
 * const defaults = {
 *   page: 1,
 *   pageSize: DEFAULT_PAGE_SIZE,
 *   sort: "-createdAt",
 *   search: undefined,
 *   played: undefined,
 *   isAuthor: undefined,
 * };
 * const parse = (query) => ({
 *   page: parsePage(query.page),
 *   pageSize: parsePageSize(query.pageSize),
 *   sort: parseSort(query.sort, SORT_KEYS, defaults.sort),
 *   search: parseText(query.search),
 *   played: parseBoolean(query.played),
 *   isAuthor: parseBoolean(query.isAuthor),
 * });
 * const { params, update, isFiltered, reset } = useListQuery({
 *   parse,
 *   defaults,
 *   filterKeys: ["search", "played", "isAuthor"],
 * });
 *
 * update({ played: false }, { resetPage: true }); // → ?played=false
 * update({ search: "history" }, { resetPage: true, replace: true }); // no new history entry
 * update({ page: 2 }); // → ?page=2&search=history&played=false
 * reset(); // clears search and filters, goes to page 1, keeps pageSize and sort
 *
 * @param {Object} options - How the list reads and writes its query
 * @param {(query: import("vue-router").LocationQuery) => Object} options.parse - Turns the route query into
 *   typed params with defaults applied. Invalid values fall back to the defaults.
 * @param {Object} options.defaults - Default value of each key `parse` returns. Values equal to their
 *   default are left out of the URL.
 * @param {string[]} options.filterKeys - Keys of the search and filters (not `page`, `pageSize` or `sort`)
 * @returns {{
 *   params: import("vue").ComputedRef<Object>,
 *   update: (patch: Object, options?: { resetPage?: boolean, replace?: boolean }) => Promise<unknown>,
 *   isFiltered: import("vue").ComputedRef<boolean>,
 *   reset: () => Promise<unknown>,
 * }} - `update` merges `patch` into the params and navigates (`router.push`, or `router.replace` with
 *   `replace`); `resetPage` also goes to page 1. A key set to `undefined` goes back to its default.
 *   `isFiltered` is true when any filter key differs from its default. `reset` clears the filter keys.
 */
export function useListQuery({ parse, defaults, filterKeys }) {
  const route = useRoute();
  const router = useRouter();

  // Keep the last params while navigating away, so the list doesn't refetch with defaults before it unmounts.
  const listRouteName = route.name;
  const params = computed((previous) => (previous && route.name !== listRouteName ? previous : parse(route.query)));

  const isFiltered = computed(() => filterKeys.some((key) => !isSameValue(params.value[key], defaults[key])));

  const update = (patch, { resetPage = false, replace = false } = {}) => {
    const next = { ...params.value, ...patch };
    if (resetPage) {
      next.page = 1;
    }
    const location = { query: toQuery(next, defaults) };
    return replace ? router.replace(location) : router.push(location);
  };

  const reset = () =>
    update(Object.fromEntries(filterKeys.map((key) => [key, undefined])), {
      resetPage: true,
    });

  return { params, update, isFiltered, reset };
}
