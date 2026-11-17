# Server-side list paging with allowed sort keys and no filtering library

The tournaments and topics lists are paged, filtered and sorted on the server. Each list is a `GET` with a query string (`page`, `pageSize`, `sort`, plus filters), and the response is a `PageResponse<T>` envelope. Paging is by offset and numbered from 1. Each list declares a fixed set of sort keys, each mapped to an expression, and every sort ends with `Id` as a tie-breaker. The search box uses an escaped `ILIKE`. We wrote this ourselves as a small set of helpers in `Cbo.Core/Paging`. The pattern comes from a larger project's filter classes (each filter's `ApplyTo` method adds its own `Where` clauses). We adapted it to two lists and left out the base-class hierarchy that project used.

## Considered options

- **Client-side paging** (PrimeVue `DataTable` with `paginator`/`filters` over the full list): no backend work, but every request returns everything the user has, and the endpoints stop scaling as data grows.
- **Keyset (cursor) paging**: stable under concurrent writes and fast on deep pages, but it can't jump to page N. Every user-selectable sort key would also need a cursor encoding. Our lists are small. We kept the tie-breaker, so keyset can still be added per list later.
- **Gridify, Sieve, OData or System.Linq.Dynamic.Core**: these accept a generic filter string from the client and give less control over what gets translated to SQL. The client could also filter and sort on any entity member.
- **Ardalis.Specification**: a formal version of the same idea. It isn't worth a dependency for two lists.
- **Sorting by any entity member, chosen by reflection** (what the reference project does): a typo becomes a 500, internal property names leak into the API, and without a tie-breaker the row order between pages isn't guaranteed.

## Consequences

- A sort key the list doesn't declare is a 400, not a 500. The frontend column's `sortField` must equal the server key.
- `GET /Topics` returns one page at a time. Callers that need everything, such as `TournamentTopicsPanel`, have to ask for the 100-row maximum and live with the limit until they get their own paging.
- Filters must translate to SQL. Unit tests check this with `ToQueryString()` against the Npgsql provider. LINQ-to-Objects or the EF InMemory provider would hide a filter that can't be translated.
