# Artifacts & Releases

## Pack Xws.Core

```
scripts/pack.ps1
# or
./scripts/pack.sh
```

Outputs:
- artifacts/nuget/Xws.Core.<version>.nupkg

## Publish xws

```
scripts/publish.ps1
# or
./scripts/publish.sh
```

Outputs:
- artifacts/publish/win-x64
- artifacts/publish/linux-x64

## WSL note

On Windows, the `.sh` scripts require WSL. Use the `.ps1` scripts if WSL is not installed.
