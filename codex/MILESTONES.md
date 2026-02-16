# MILESTONES

## Milestone 11: Repository Restructure
Status: IN PROGRESS

Definition of Done:
- Solution builds with zero errors.
- Tests pass: `Xws.Data.Tests`, `Xws.Exec.Tests`, `Xws.Exchanges.Tests`.
- CLI works: `dotnet run --project src/Xws -- --help`.
- Benchmarks run: `dotnet run --project benchmarks/Xws.Benchmarks -c Release`.
- Python demo works from `tools/demo`.
- Clean source tree (no `bin/` or `obj/` in `src/`).
- All project references updated.
- README paths updated.

Acceptance Tests:
- `dotnet clean`
- `dotnet build`
- `dotnet test --no-build`
- `dotnet run --project src/Xws -- --help`
- `tree src/ -I 'bin|obj' -L 2`
