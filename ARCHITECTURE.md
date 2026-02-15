# Architecture

## Overview
`xws` is a .NET 8 CLI and library set for exchange WebSocket market data ingestion and (paper/testnet/mainnet) execution workflows. The system is library-first:
- `Xws.Core` provides exchange adapters, muxing, envelope serialization, and WebSocket runners.
- `xws` is a thin CLI that wires commands to `Xws.Core`.
- `Xws.Exec` provides execution abstractions and paper-state handling.
- `xws.exec.cli` is a thin CLI over `Xws.Exec`.

## Module Diagram
```mermaid
flowchart LR
  subgraph CLI
    XWS[xws CLI]
    XWS_EXEC[xws.exec.cli]
  end
  subgraph Core
    CORE[Xws.Core]
    MUX[MuxRunner]
    WS[WebSocketRunner]
  end
  subgraph Exec
    EXEC[Xws.Exec]
    STATE[PaperStateStore]
  end
  XWS --> CORE
  XWS_EXEC --> EXEC
  CORE --> MUX
  CORE --> WS
  EXEC --> STATE
```

## Data Flow
```mermaid
flowchart TD
  WS_CONN[WebSocket Connection] --> PARSER[Exchange Parser]
  PARSER --> ENVELOPE[EnvelopeV1]
  ENVELOPE --> MUX[MuxRunner]
  MUX --> STDOUT[JSONL stdout]

  CLI[xws CLI] --> WS_CONN
  CLI --> MUX
```

## Interfaces
- `IExchangeAdapter`: encapsulates exchange-specific subscription and parsing behaviors.
- `IWebSocketClient`: minimal WebSocket interface for exchange clients.
- `IMessageParser`: exchange message-to-envelope conversion.
- `IExecutionClient`: execution surface for place/cancel/amend/query operations.

## Execution State Machine
```mermaid
stateDiagram-v2
  [*] --> Pending
  Pending --> Open: accepted
  Open --> Filled: fill
  Open --> Cancelled: cancel
  Pending --> Rejected: reject
  Open --> Rejected: reject
```

## Repository Structure (Key Paths)
- `src/Xws.Core/Exchanges/{HL,OKX,Bybit,MEXC}/WebSocket/`
- `src/Xws.Core/Shared/Interfaces/`
- `src/Xws.Exec/Exchanges/{HL,OKX,Bybit}/`
- `src/Xws.Exec/Shared/State/`
- `src/xws/Commands/`
- `src/xws.exec.cli/Commands/`
