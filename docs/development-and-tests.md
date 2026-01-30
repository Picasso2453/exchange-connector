# Development & Tests

## Build

```
dotnet build -c Release
```

## Tests (offline-safe)

```
dotnet test -c Release
```

## Stdout integrity check

```
dotnet run --project src/xws -- dev emit --count 2 --timeout-seconds 2 1>out.txt 2>err.txt
```

## Notes

- Core never writes to console; CLI owns stdout/stderr.
- CI runs build/test on Windows + Linux without live endpoints.
