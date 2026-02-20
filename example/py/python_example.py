import sys
from pathlib import Path

# Requires: pip install pythonnet
# Build the DLL first:
#   dotnet build .\Connector.sln
# Tip: set HL_NETWORK=testnet and HL_USER_ADDRESS in src/.env for safe testing.

def _load_dll():
    # Resolve the built Connector.Core.dll (net8.0) relative to this script.
    dll_path = Path(
        "C:/projects/exchange-connector/src/Connector.Core/bin/Debug/net8.0/Connector.Core.dll"
    )

    if not dll_path.exists():
        raise FileNotFoundError(
            f"Connector.Core.dll not found at {dll_path}. "
            "Build the solution first."
        )

    # Make the DLL discoverable by pythonnet (CLR loader).
    sys.path.append(str(dll_path.parent))

    import pythonnet  # type: ignore
    # Force CoreCLR so net8.0 types like IAsyncDisposable resolve correctly.
    runtime_config = Path(
        "C:/projects/exchange-connector/src/Connector.Cli/bin/Debug/net8.0/Connector.Cli.runtimeconfig.json"
    )
    if runtime_config.exists():
        pythonnet.load("coreclr", runtime_config=str(runtime_config))
    else:
        pythonnet.load("coreclr")
    import clr  # type: ignore
    import System

    # Load the .NET assembly into the CLR by absolute path.
    clr.AddReference(str(dll_path))
    # Load core framework assemblies used by the script.
    clr.AddReference("System.Net.Http")
    # Load logging abstractions if not already in the load context.
    logging_dll = _find_nuget_logging_abstractions()
    if logging_dll is None:
        logging_dll = _find_dotnet_shared(
            "Microsoft.Extensions.Logging.Abstractions.dll",
            preferred_major=8,
        )
    if logging_dll is not None:
        # Ensure the loader can resolve the assembly by path.
        sys.path.append(str(logging_dll.parent))
        clr.AddReference(str(logging_dll))

    # Cache the loaded Connector.Core assembly for reflection access.
    global _CONNECTOR_ASSEMBLY
    _CONNECTOR_ASSEMBLY = next(
        a for a in System.AppDomain.CurrentDomain.GetAssemblies()
        if a.GetName().Name == "Connector.Core"
    )


def _find_dotnet_shared(assembly_name: str, preferred_major: int | None = None) -> Path | None:
    """Best-effort locator for shared framework assemblies on Windows."""
    dotnet_root = Path("C:/Program Files/dotnet/shared")
    if not dotnet_root.exists():
        return None

    for framework in ("Microsoft.AspNetCore.App", "Microsoft.NETCore.App"):
        framework_root = dotnet_root / framework
        if not framework_root.exists():
            continue
        # Pick the highest version available.
        candidates = sorted(framework_root.iterdir(), reverse=True)
        if preferred_major is not None:
            candidates = [p for p in candidates if p.name.startswith(f"{preferred_major}.")]
        for version_dir in candidates:
            candidate = version_dir / assembly_name
            if candidate.exists():
                return candidate

    return None


def _find_nuget_logging_abstractions() -> Path | None:
    """Find the NuGet package DLL to match the project's package reference."""
    base = Path.home() / ".nuget" / "packages" / "microsoft.extensions.logging.abstractions"
    if not base.exists():
        return None

    for version_dir in sorted(base.iterdir(), reverse=True):
        candidate = version_dir / "lib" / "net8.0" / "Microsoft.Extensions.Logging.Abstractions.dll"
        if candidate.exists():
            return candidate

    return None


def _get_null_logger_factory():
    """Return NullLoggerFactory.Instance via reflection to avoid namespace import issues."""
    from System import Type

    t = Type.GetType(
        "Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory, "
        "Microsoft.Extensions.Logging.Abstractions"
    )
    if t is None:
        raise RuntimeError("Failed to resolve NullLoggerFactory type")

    instance_field = t.GetField("Instance")
    if instance_field is None:
        raise RuntimeError("NullLoggerFactory.Instance not found")

    return instance_field.GetValue(None)


def _get_null_logger(generic_type):
    """Return NullLogger<T>.Instance for the given T."""
    from System import Type

    generic_def = Type.GetType(
        "Microsoft.Extensions.Logging.Abstractions.NullLogger`1, "
        "Microsoft.Extensions.Logging.Abstractions"
    )
    if generic_def is None:
        raise RuntimeError("NullLogger`1 type not found")

    concrete = generic_def.MakeGenericType(generic_type)
    instance_field = concrete.GetField("Instance")
    if instance_field is None:
        raise RuntimeError("NullLogger<T>.Instance not found")

    return instance_field.GetValue(None)


def _get_type(full_name: str):
    """Resolve a .NET type from Connector.Core via reflection."""
    t = _CONNECTOR_ASSEMBLY.GetType(full_name)
    if t is None:
        raise RuntimeError(f"Type not found: {full_name}")
    return t


def main():
    # 1) Load the .NET DLL so we can import Connector.Core namespaces.
    _load_dll()

    # 2) Import .NET types and the connector library types.
    from System import Array, Guid, String, Uri, Activator, Enum, TimeSpan, Int32, Decimal
    from System.Net.Http import HttpClient
    from System.Threading import CancellationTokenSource

    # Resolve Connector.Core types via reflection (pythonnet namespace import can be flaky).
    PlaceOrderRequest = _get_type("Connector.Core.Contracts.PlaceOrderRequest")
    UnifiedExchange = _get_type("Connector.Core.Contracts.UnifiedExchange")
    UnifiedWsChannel = _get_type("Connector.Core.Contracts.UnifiedWsChannel")
    UnifiedWsSubscribeRequest = _get_type("Connector.Core.Contracts.UnifiedWsSubscribeRequest")

    HyperliquidAdapter = _get_type("Connector.Core.Exchanges.Hyperliquid.HyperliquidAdapter")
    HyperliquidAuthProvider = _get_type("Connector.Core.Exchanges.Hyperliquid.HyperliquidAuthProvider")
    HyperliquidConfig = _get_type("Connector.Core.Exchanges.Hyperliquid.HyperliquidConfig")
    NoAuthProvider = _get_type("Connector.Core.Exchanges.Hyperliquid.NoAuthProvider")

    RestManager = _get_type("Connector.Core.Managers.RestManager")
    WebSocketManager = _get_type("Connector.Core.Managers.WebSocketManager")
    RestTransport = _get_type("Connector.Core.Transport.RestTransport")
    WsTransport = _get_type("Connector.Core.Transport.WsTransport")
    TokenBucketRateLimiter = _get_type("Connector.Core.Transport.TokenBucketRateLimiter")

    # 3) Read Hyperliquid config from environment variables.
    config = HyperliquidConfig.GetMethod("FromEnvironment").Invoke(None, None)
    logger_factory = _get_null_logger_factory()

    # 4) Create the adapter (translators for REST/WS).
    adapter = Activator.CreateInstance(HyperliquidAdapter, [config, logger_factory, False])

    # --- WebSocket: market data streams ---
    # Use the no-op auth provider because these are public market data streams.
    rate_limiter = Activator.CreateInstance(
        TokenBucketRateLimiter,
        [Int32(1000), TimeSpan.FromSeconds(1)],
    )

    ws_transport = Activator.CreateInstance(
        WsTransport,
        [_get_null_logger(WsTransport)],
    )
    ws_manager = Activator.CreateInstance(
        WebSocketManager,
        [
            ws_transport,
            adapter.CreateWsTranslator(),
            Activator.CreateInstance(NoAuthProvider),
            _get_null_logger(WebSocketManager),
            rate_limiter,
            Int32(10000),
        ],
    )

    # Single cancellation token for the script lifetime.
    cts = CancellationTokenSource()
    ws_manager.StartAsync(Uri(config.WsUrl), cts.Token).Wait()

    symbol = "BTC"
    exchange_hl = Enum.Parse(UnifiedExchange, "Hyperliquid")
    streams = [
        (Enum.Parse(UnifiedWsChannel, "Trades"), None),
        (Enum.Parse(UnifiedWsChannel, "OrderBookL1"), None),
        (Enum.Parse(UnifiedWsChannel, "OrderBookL2"), None),
        (Enum.Parse(UnifiedWsChannel, "Candles"), "1m"),
        (Enum.Parse(UnifiedWsChannel, "AllMids"), None),
        (Enum.Parse(UnifiedWsChannel, "ActiveAssetCtx"), None),
    ]

    # Subscribe to each public market data channel once.
    for channel, interval in streams:
        req = Activator.CreateInstance(UnifiedWsSubscribeRequest)
        req.CorrelationId = Guid.NewGuid().ToString()
        req.Exchange = exchange_hl
        req.Channel = channel
        req.Symbols = Array[String]([symbol])
        req.Interval = interval
        ws_manager.SubscribeAsync(req, cts.Token).Wait()

    print("Subscribed to market data streams. Waiting for one event per stream...")

    reader = ws_manager.GetEventReader()
    seen = set()
    deadline_ms = 10000
    start = __import__("time").time()

    # Read until we see at least one event per stream (or timeout).
    while len(seen) < len(streams) and (time := __import__("time").time() - start) * 1000 < deadline_ms:
        evt = reader.ReadAsync(cts.Token).AsTask().Result
        seen.add(str(evt.Channel))
        print(f"{evt.Channel} -> {evt.Symbol}")

    # --- REST: place a market buy and a limit sell ---
    # NOTE: Trading endpoints and EIP-712 signing are not yet implemented in
    # HyperliquidRestTranslator / HyperliquidAuthProvider.
    # The calls below demonstrate how to construct the requests and will fail
    # until signing and exchange endpoints are wired up.

    # REST client setup (base address points to HL REST endpoint).
    http_client = HttpClient()
    http_client.BaseAddress = Uri(config.HttpUrl)

    # REST manager wires up transport + translator + auth provider.
    rest_manager = Activator.CreateInstance(
        RestManager,
        [
            Activator.CreateInstance(
                RestTransport,
                [http_client, _get_null_logger(RestTransport)],
            ),
            adapter.CreateRestTranslator(),
            Activator.CreateInstance(HyperliquidAuthProvider, [config]),
            _get_null_logger(RestManager),
            rate_limiter,
        ],
    )

    # Example market buy.
    market_buy = Activator.CreateInstance(PlaceOrderRequest)
    market_buy.Exchange = exchange_hl
    market_buy.Symbol = symbol
    market_buy.Side = "buy"
    market_buy.OrderType = "market"
    market_buy.Size = Decimal.Parse("0.001")

    # Example limit sell (price is intentionally far to avoid accidental fills).
    limit_sell = Activator.CreateInstance(PlaceOrderRequest)
    limit_sell.Exchange = exchange_hl
    limit_sell.Symbol = symbol
    limit_sell.Side = "sell"
    limit_sell.OrderType = "limit"
    limit_sell.Size = Decimal.Parse("0.001")
    limit_sell.Price = Decimal.Parse("999999")

    try:
        market_resp = rest_manager.ExecuteAsync(market_buy, cts.Token).Result
        print(f"Market buy order id: {market_resp.OrderId}")

        limit_resp = rest_manager.ExecuteAsync(limit_sell, cts.Token).Result
        print(f"Limit sell order id: {limit_resp.OrderId}")
    except Exception as ex:
        print("Order placement failed (expected until trading support is implemented):")
        print(ex)
    finally:
        # Clean shutdown for WS and HTTP resources.
        ws_manager.StopAsync().Wait()
        ws_manager.DisposeAsync().AsTask().Wait()
        http_client.Dispose()


if __name__ == "__main__":
    main()
