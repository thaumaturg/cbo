# Request validation with FluentValidation in an endpoint filter, before access checks

Rules that need only the request itself (lengths, ranges, counts, required fields, duplicates within the body) are FluentValidation validators for the request DTOs. One endpoint filter on the `/api` group runs them before the handler is called. Rules that need the database (an account exists, the owner isn't a co-author, the owned-topics cap, a played topic, a topic belongs to the tournament, the tournament's own topic maximum) stay in the handlers. As a result, an invalid body on `PUT /Topics/{id}` or `PUT /Tournaments/{id}` gets a 400 even when the resource doesn't exist or the user may not edit it. This fits ADR 0002's definition of Invalid ("wrong in itself, whatever the current data looks like"), and it replaces the "access, then validation, then state" order that ADR 0003 describes, but only for stateless checks. Before this, those rules were spread across hand-written services and handlers, and some limits weren't enforced at all.

Text fields are trimmed in the request DTO setters (C# 14 `field`), during deserialization, so validators see exactly the value that gets saved. Passwords are never trimmed.

## Considered Options

- **.NET 10 built-in Minimal API validation (`AddValidation`, DataAnnotations)**: rejected because DataAnnotations can't express cross-field and collection rules (sequential question numbers, total cost, min ≤ max, duplicates) without custom attributes or `IValidatableObject`. We'd end up writing half of the rules by hand again.
- **Validators on commands, called inside each handler after the access check**: keeps the ADR 0003 order, but every rule would need `OverridePropertyName` to turn command names back into JSON paths (`CoAuthorUsernames` → `authors[i].username`), and every handler would need the same call.
- **A global `JsonConverter<string>` that trims everything**: rejected because it's invisible when reading a DTO, and passwords would need an opt-out attribute that's easy to forget.

## Consequences

- A request DTO with a validator is validated automatically. The filter finds validators by argument type, so a new endpoint can't forget to call it.
- Field error keys stay camelCase JSON paths (`questions[0].text`, `topics[2].title`, `answers[3].overrideCost`), thanks to a global property-name resolver.
