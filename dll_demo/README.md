# ExecGuard DLL Demo UI

This demo provides a minimal, standalone Python UI that showcases a typical ExecGuard DLL/library usage pattern (connect, get symbols, subscribe to a data stream, send an order) using the built .NET assemblies.

## Run

```bash
pip install pythonnet
dotnet build -c Release
cd dll_demo
python demo_ui.py
```

## Expected Behavior
- A window appears with exchange, symbol, datastream, side, and order type controls.
- Selecting an exchange loads symbols for that exchange.
- Clicking **Send Trade** shows a library-backed order result in the status area.

## Notes
- This uses `pythonnet` to load `Xws.Core.dll` and `Xws.Exec.dll` built from this repo.
- Build the DLLs first with `dotnet build -c Release`.
- The demo loads assemblies from `src/xws/bin/Release/net8.0` and `src/xws.exec.cli/bin/Release/net8.0` (these outputs include dependencies).
- For live HL execution, set `XWS_HL_USER`, `XWS_HL_PRIVATE_KEY`, `XWS_EXEC_MODE`, and `XWS_EXEC_ARM=1`.
