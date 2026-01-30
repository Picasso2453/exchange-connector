# CLI Usage

## Hyperliquid

Trades:
```
dotnet run --project src/xws -- hl subscribe trades --symbol SOL
```

Positions (requires env var):
```
$env:XWS_HL_USER="0xYourAddressHere"
dotnet run --project src/xws -- hl subscribe positions --max-messages 10 --timeout-seconds 30
```

## MEXC

Spot trades:
```
dotnet run --project src/xws -- mexc spot subscribe trades --symbol BTCUSDT --max-messages 10 --timeout-seconds 30
```

## Mux (multi-exchange)

Trades:
```
dotnet run --project src/xws -- subscribe trades --sub hl=SOL --sub mexc.spot=BTCUSDT --max-messages 50 --timeout-seconds 30
```

Futures trades (MEXC):
```
dotnet run --project src/xws -- subscribe trades --sub mexc.fut=BTC_USDT --max-messages 10 --timeout-seconds 15
```

L2 (MEXC futures):
```
dotnet run --project src/xws -- subscribe l2 --sub mexc.fut=BTC_USDT --max-messages 5 --timeout-seconds 15
```
