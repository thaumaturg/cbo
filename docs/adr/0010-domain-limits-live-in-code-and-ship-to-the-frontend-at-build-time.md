# Domain limits live in code and ship to the frontend at build time

Limits and tournament defaults are C# constants in `Cbo.Core`, not `appsettings.json` values. The frontend gets them from a JSON file that a backend snapshot test generates and that is committed to the repo. They aren't in configuration because they are tied to code and saved data, not to the environment. Changing `RoundsPerQualificationMatch` would make every finished match look unfinished. Lowering a question-cost cap would make existing topics impossible to save. A tournament size has to match a qualification table that exists only in code. The values don't come from a runtime endpoint because the frontend and backend ship in one image, so values fixed at build time can't drift apart. An endpoint would only add a request that blocks startup, a failure state, and the risk of a cached copy going stale after a deploy.

## Considered Options

- **`appsettings.json` with the Options pattern**: rejected because an operator could change a rule that saved data depends on. Values that really do vary by environment, like the JWT lifetime, still go there.
- **`GET /api/limits` cached in Pinia/localStorage**: rejected for the reasons above. Revisit only if a limit has to change without a redeploy.
- **Limits as an `x-limits` extension in the OpenAPI document, or an MSBuild export step**: rejected because each needs more machinery than a snapshot test for the same result.

## Consequences

- Changing a limit means editing the constant, running the tests, and committing the regenerated JSON. A stale JSON file fails the test run.
- `appsettings.json` stays limited to environment concerns: connection strings, JWT settings and logging.
