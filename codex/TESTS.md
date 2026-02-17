# TESTS.md

## Build verification
```bash
dotnet build
dotnet test
```

Expected: 41 tests passing, 0 failures.

## CLI smoke (no-auth, public)
```bash
dotnet run --project src/Connector.Cli -- --help
dotnet run --project src/Connector.Cli -- --version
```

## Live smoke — Trades (requires network, no auth)
```bash
dotnet run --project src/Connector.Cli -- --exchange hl --symbols BTC --channels trades --no-auth
```

## Live smoke — L2 Orderbook
```bash
dotnet run --project src/Connector.Cli -- --exchange hl --symbols SOL --channels l2 --no-auth
```

## Live smoke — Candles
```bash
dotnet run --project src/Connector.Cli -- --exchange hl --symbols BTC --channels candles --no-auth
```

## Live smoke — Multiple channels/symbols
```bash
dotnet run --project src/Connector.Cli -- --exchange hl --symbols BTC,ETH,SOL --channels trades,l2 --no-auth
```

## Live smoke — With raw payloads
```bash
dotnet run --project src/Connector.Cli -- --exchange hl --symbols BTC --channels trades --no-auth --raw
```

## Auth smoke (requires HL_USER_ADDRESS env var)
```bash
export HL_USER_ADDRESS=0xYourAddress
dotnet run --project src/Connector.Cli -- --exchange hl --symbols BTC --channels userOrders
```

## Soak tests (automated, in test suite)
The soak tests run automatically as part of `dotnet test`:
- `Soak_FakeTransport_ProcessesEventsForDuration` — 3s pipeline with fake transport
- `Soak_MultipleChannels_NoDeadlock` — 2s multi-symbol pipeline
- `Soak_HighThroughput_NoCrash` — 2s high-throughput pipeline
