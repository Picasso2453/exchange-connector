# PROJECT

## Intent
Restructure the repository into clear layers: market data (`Xws.Data`), execution (`Xws.Exec`), shared exchange adapters (`Xws.Exchanges`), and a unified CLI (`Xws`).

## Scope (Milestone 11)
- Create `Xws.Exchanges` project and move exchange adapters from `Xws.Data`.
- Rename `Xws.Data` to `Xws.Data` (market data layer).
- Merge `xws` and `xws.exec.cli` into a single `Xws` CLI project.
- Keep `Xws.Exec` as-is (execution layer), update references.
- Move `dll_demo/` to `tools/demo/`, `scripts/` to `tools/scripts/`.
- Move `project-notes/` to `.archive/milestone-notes/`.
- Remove `.venv312/`, keep `.venv/` and update `.gitignore`.
- Rename tests: `xws.tests` -> `Xws.Data.Tests`; create `Xws.Exchanges.Tests` for exchange fixtures.
- Update solution file, namespaces, and project references.

## Constraints
- .NET 8 target and existing 4-project architecture.
- No new features or dependency updates.
- No internal logic changes; refactor only.
- Preserve NuGet APIs (rename only, no breaking behavior).
- Python demo must still run after path changes.

## Milestones
- Milestone 11: Repository restructure and validation.

## Status
- Started: 2026-02-16
