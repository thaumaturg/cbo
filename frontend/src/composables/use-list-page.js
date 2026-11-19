import { parseText } from "@/composables/use-list-query.js";
import { useNotify } from "@/utils/notify.js";
import { computed, onBeforeUnmount, ref, watch } from "vue";

const SEARCH_DELAY_MS = 300;

/**
 * Load a server-paged list whenever its URL state changes, and handle the list's search box, paginator
 * and sorting. Builds on {@link useListQuery}: the URL stays the only source of truth, and every handler
 * here writes to it through `update` or `reset`.
 *
 * - Each change to `params` loads the page, cancelling the request in flight, so a slower old response
 *   can't overwrite a newer one. The previous rows stay visible while loading.
 * - A page past the end of a non-empty list moves to the last page without a new history entry.
 * - The search box has its own text, written to the URL 300 ms after typing stops.
 *
 * Call it from the `setup` of a component rendered by a route.
 *
 * @example
 * const listQuery = useListQuery({ parse, defaults, filterKeys: ["search", "played", "isAuthor"] });
 * const { items, isLoading, paging, searchText, handleSearchInput, handlePage, handleSort, clearFilters } =
 *   useListPage({ listQuery, defaultSort: defaults.sort, load: topicService.getTopics });
 *
 * @param {Object} options - The list to load
 * @param {ReturnType<typeof import("@/composables/use-list-query.js").useListQuery>} options.listQuery - The
 *   list's URL state
 * @param {string} options.defaultSort - The list's default sort. It shows no sort indicator in the table.
 * @param {(params: Object, options: { signal: AbortSignal }) => Promise<{ success: boolean, data?: Object,
 *   message?: string }>} options.load - The service call. `data` is a page: `{ items, page, pageSize, totalCount }`.
 * @returns {{
 *   items: import("vue").Ref<Object[]>,
 *   totalCount: import("vue").Ref<number>,
 *   isLoading: import("vue").Ref<boolean>,
 *   paging: import("vue").ComputedRef<{ page: number, pageSize: number, totalCount: number, sort?: string }>,
 *   searchText: import("vue").Ref<string>,
 *   handleSearchInput: (text: string | undefined) => void,
 *   handlePage: (event: { page: number, pageSize: number }) => void,
 *   handleSort: (event: { sort?: string }) => void,
 *   clearFilters: () => void,
 * }} - `paging` goes to the table's `paging` prop, and the handlers to its `page`, `sort` and
 *   `clear-filters` events and the search box's `update:modelValue`
 */
export function useListPage({ listQuery, defaultSort, load }) {
  const { params, update, reset } = listQuery;
  const notify = useNotify();

  const items = ref([]);
  const totalCount = ref(0);
  const isLoading = ref(false);

  // The default order shows no sort indicator (see the tables' `paging` prop)
  const paging = computed(() => ({
    page: params.value.page,
    pageSize: params.value.pageSize,
    totalCount: totalCount.value,
    sort: params.value.sort === defaultSort ? undefined : params.value.sort,
  }));

  let requestController;

  const fetchPage = async (listParams) => {
    requestController?.abort();
    const controller = new AbortController();
    requestController = controller;

    isLoading.value = true;
    const result = await load(listParams, { signal: controller.signal });
    // A newer request replaced this one, or the view unmounted: its result is stale, not an error
    if (controller.signal.aborted) return;

    isLoading.value = false;
    if (result.success) {
      items.value = result.data.items;
      totalCount.value = result.data.totalCount;
      // A page past the end of a non-empty list goes to the last page. PrimeVue's paginator only does that when the
      // total changes. It can't loop: the last page isn't past the end.
      if (result.data.items.length === 0 && result.data.totalCount > 0) {
        update({ page: Math.ceil(result.data.totalCount / listParams.pageSize) }, { replace: true });
      }
    } else {
      notify.error("Load Failed", result.message);
    }
  };

  watch(params, fetchPage, { immediate: true });

  // The search box has its own text, written to the URL once typing stops
  const searchText = ref(params.value.search ?? "");
  let searchTimer;

  const cancelPendingSearch = () => {
    clearTimeout(searchTimer);
    searchTimer = undefined;
  };

  const handleSearchInput = (text) => {
    searchText.value = text ?? "";
    cancelPendingSearch();
    searchTimer = setTimeout(() => {
      searchTimer = undefined;
      const search = parseText(searchText.value);
      if (search !== params.value.search) {
        update({ search }, { resetPage: true, replace: true });
      }
    }, SEARCH_DELAY_MS);
  };

  // Back/Forward and Clear filters change the search in the URL; only sync the box when the user isn't typing
  watch(
    () => params.value.search,
    (search) => {
      if (searchTimer === undefined && parseText(searchText.value) !== search) {
        searchText.value = search ?? "";
      }
    },
  );

  const handlePage = ({ page, pageSize }) => {
    // Once the total arrives, PrimeVue's paginator moves a page past the end to the last page.
    // That isn't a step the user took, so it replaces the URL instead of adding a history entry.
    const isPastEnd = (params.value.page - 1) * params.value.pageSize >= totalCount.value;
    // A new page size starts again from the first page
    update({ page, pageSize }, { resetPage: pageSize !== params.value.pageSize, replace: isPastEnd });
  };

  const handleSort = ({ sort }) => {
    update({ sort }, { resetPage: true });
  };

  const clearFilters = () => {
    cancelPendingSearch();
    searchText.value = "";
    reset();
  };

  onBeforeUnmount(() => {
    cancelPendingSearch();
    requestController?.abort();
  });

  return {
    items,
    totalCount,
    isLoading,
    paging,
    searchText,
    handleSearchInput,
    handlePage,
    handleSort,
    clearFilters,
  };
}
