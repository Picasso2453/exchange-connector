"""ExecGuard library adapter for demo UI.

This module loads the built .NET libraries (Xws.Core/Xws.Exec) via pythonnet
and exposes a minimal trading interface for the demo UI.
"""
from __future__ import annotations
from typing import Dict, List
from pathlib import Path
import json
import os
import threading

try:
    from pythonnet import load  # type: ignore
except ImportError as exc:
    raise RuntimeError(
        "pythonnet is required. Install with: pip install pythonnet"
    ) from exc


_DATASTREAMS = ["trades", "l2", "funding", "liquidations", "markprice", "fills"]
_EXCHANGES = ["hl", "okx", "bybit", "mexc"]

_loaded = False
_core_asm = None
_exec_asm = None
clr = None
System = None


def _load_assemblies() -> None:
    global _loaded, _core_asm, _exec_asm, clr, System
    if _loaded:
        return

    root = Path(__file__).resolve().parents[1]
    runtime_config = root / "src" / "xws" / "bin" / "Release" / "net8.0" / "xws.runtimeconfig.json"
    if not runtime_config.exists():
        raise FileNotFoundError(
            "Missing xws.runtimeconfig.json. Build with: dotnet build -c Release"
        )
    load("coreclr", runtime_config=str(runtime_config))
    import clr as _clr  # type: ignore
    import System as _System  # type: ignore
    clr = _clr
    System = _System
    core_dir = root / "src" / "xws" / "bin" / "Release" / "net8.0"
    exec_dir = root / "src" / "xws.exec.cli" / "bin" / "Release" / "net8.0"
    core_dll = core_dir / "Xws.Core.dll"
    exec_dll = exec_dir / "Xws.Exec.dll"

    if not core_dll.exists() or not exec_dll.exists():
        raise FileNotFoundError(
            "Missing Xws.Core.dll or Xws.Exec.dll. Build with: dotnet build -c Release"
        )

    os.add_dll_directory(str(core_dir))
    os.add_dll_directory(str(exec_dir))
    clr.AddReference(str(core_dll))
    clr.AddReference(str(exec_dll))
    _core_asm = System.Reflection.Assembly.LoadFile(str(core_dll))
    _exec_asm = System.Reflection.Assembly.LoadFile(str(exec_dll))
    _loaded = True


def _get_type(full_name: str, assembly) -> "System.Type":
    if assembly is None:
        raise RuntimeError("Assemblies not loaded")
    t = assembly.GetType(full_name)
    if t is None:
        raise RuntimeError(f"Missing type: {full_name}")
    return t


def connect(exchange: str) -> bool:
    """Connect to the library for the selected exchange."""
    _load_assemblies()
    exchange = (exchange or "").lower()
    if exchange not in _EXCHANGES:
        return False

    if exchange == "hl":
        config_type = _get_type(
            "xws.Exchanges.Hyperliquid.HyperliquidConfig", _core_asm
        )
        load_method = config_type.GetMethod("Load")
        _ = load_method.Invoke(None, None)
        return True

    return True


def get_symbols(exchange: str) -> List[str]:
    """Fetch symbols for the exchange using library endpoints when available."""
    _load_assemblies()
    exchange = (exchange or "").lower()
    if exchange != "hl":
        return []

    from System.Threading import CancellationToken
    config_type = _get_type("xws.Exchanges.Hyperliquid.HyperliquidConfig", _core_asm)
    load_method = config_type.GetMethod("Load")
    config = load_method.Invoke(None, None)

    rest_type = _get_type("xws.Exchanges.Hyperliquid.Rest.HLRestClient", _core_asm)
    post_info = rest_type.GetMethod("PostInfoAsync")
    cancellation_none = getattr(CancellationToken, "None")
    http_uri = config.GetType().GetProperty("HttpUri").GetValue(config, None)
    payload = post_info.Invoke(
        None,
        [http_uri, "meta", cancellation_none],
    ).Result

    try:
        data = json.loads(payload)
    except json.JSONDecodeError:
        return []

    universe = data.get("universe") or data.get("coins") or data.get("symbols") or []
    symbols: List[str] = []
    for item in universe:
        if isinstance(item, dict):
            name = item.get("name") or item.get("coin") or item.get("symbol")
            if isinstance(name, str) and name:
                symbols.append(name)
        elif isinstance(item, str):
            symbols.append(item)

    return symbols


def get_datastreams() -> List[str]:
    """Return available datastreams supported by the demo UI."""
    return list(_DATASTREAMS)


def send_order(
    exchange: str,
    symbol: str,
    datastream: str,
    side: str,
    order_type: str,
    quantity: float,
) -> Dict[str, object]:
    """Send an order using Xws.Exec."""
    _load_assemblies()
    exchange = (exchange or "").lower()
    if exchange not in _EXCHANGES:
        return {"success": False, "error": "unsupported exchange"}

    if not symbol or quantity <= 0:
        return {"success": False, "error": "invalid order parameters"}

    from System.Threading import CancellationToken

    mode_value = os.getenv("XWS_EXEC_MODE", "paper").lower()
    exec_mode_type = _get_type("Xws.Exec.ExecutionMode", _exec_asm)
    if mode_value == "testnet":
        mode = System.Enum.Parse(exec_mode_type, "Testnet")
    elif mode_value == "mainnet":
        mode = System.Enum.Parse(exec_mode_type, "Mainnet")
    else:
        mode = System.Enum.Parse(exec_mode_type, "Paper")

    arm_env = os.getenv("XWS_EXEC_ARM")
    arm_live = bool(arm_env) and arm_env == "1"

    creds = None
    user = os.getenv("XWS_HL_USER")
    private_key = os.getenv("XWS_HL_PRIVATE_KEY")
    if str(mode) != "Paper" and exchange == "hl":
        if not user or not private_key:
            return {"success": False, "error": "missing XWS_HL_USER or XWS_HL_PRIVATE_KEY"}
        creds_type = _get_type("Xws.Exec.HyperliquidCredentials", _exec_asm)
        creds = System.Activator.CreateInstance(creds_type, [user, private_key])

    config_type = _get_type("Xws.Exec.ExecutionConfig", _exec_asm)
    config = System.Activator.CreateInstance(
        config_type,
        [mode, arm_live, arm_env, user, creds, None],
    )

    side_type = _get_type("Xws.Exec.OrderSide", _exec_asm)
    order_side = System.Enum.Parse(
        side_type, "Buy" if side.lower() == "buy" else "Sell"
    )
    order_type_type = _get_type("Xws.Exec.OrderType", _exec_asm)
    order_type_enum = System.Enum.Parse(
        order_type_type, "Market" if order_type.lower() == "market" else "Limit"
    )
    if order_type.lower() == "limit":
        return {"success": False, "error": "limit orders require price (demo UI does not supply price)"}

    request_type = _get_type("Xws.Exec.PlaceOrderRequest", _exec_asm)
    culture = System.Globalization.CultureInfo.InvariantCulture
    size_decimal = System.Decimal.Parse(str(quantity), culture)
    request = System.Activator.CreateInstance(
        request_type,
        [symbol, order_side, order_type_enum, size_decimal, None, None, False],
    )

    factory_type = _get_type("Xws.Exec.ExecutionClientFactory", _exec_asm)
    create_method = None
    for method in factory_type.GetMethods():
        if method.Name != "Create":
            continue
        parameters = method.GetParameters()
        if len(parameters) >= 2 and parameters[1].ParameterType.FullName == "System.String":
            create_method = method
            break
    if create_method is None:
        return {"success": False, "error": "execution client factory overload not found"}

    client = create_method.Invoke(None, [config, exchange, None, None, None])
    cancellation_none = getattr(CancellationToken, "None")
    result = client.PlaceAsync(request, cancellation_none).Result

    return {
        "success": True,
        "order_id": result.OrderId,
        "status": str(result.Status),
        "exchange": exchange,
        "symbol": symbol,
        "datastream": datastream,
        "order_type": order_type,
        "side": side,
        "quantity": quantity,
        "mode": str(result.Mode),
    }


def start_subscription(
    exchange: str,
    symbol: str,
    datastream: str,
    max_messages: int = 10,
    timeout_seconds: int = 15,
) -> Dict[str, object]:
    """Start a subscription using Xws.Core and return a handle."""
    _load_assemblies()
    exchange = (exchange or "").lower()
    datastream = (datastream or "").lower()
    if exchange not in _EXCHANGES:
        raise ValueError("unsupported exchange")
    if not symbol:
        raise ValueError("symbol required")

    runner_type = _get_type("xws.Core.Runner.XwsRunner", _core_asm)
    runner = System.Activator.CreateInstance(runner_type)
    output = runner.GetType().GetProperty("Output").GetValue(runner, None)
    reader = output.GetType().GetProperty("Reader").GetValue(output, None)

    task = None
    cancellation_none = getattr(System.Threading.CancellationToken, "None")
    time_span = System.TimeSpan

    options_type = _get_type("xws.Core.Mux.MuxRunnerOptions", _core_asm)
    options = System.Activator.CreateInstance(options_type)
    options.MaxMessages = max_messages
    options.Timeout = time_span.FromSeconds(timeout_seconds)

    market = None
    if exchange in ("okx", "bybit", "mexc"):
        market = "fut"

    sub_type = _get_type("xws.Core.Mux.MuxSubscription", _core_asm)
    subs_list_type = System.Collections.Generic.List[sub_type]
    subs = subs_list_type()
    symbols_array = System.Array[System.String]([symbol])
    subs.Add(System.Activator.CreateInstance(sub_type, [exchange, market, symbols_array]))

    method_name = {
        "trades": "RunMuxTradesAsync",
        "l2": "RunMuxL2Async",
        "funding": "RunMuxFundingAsync",
        "liquidations": "RunMuxLiquidationsAsync",
        "markprice": "RunMuxMarkPriceAsync",
        "fills": "RunMuxFillsAsync",
    }.get(datastream)
    if method_name is None:
        raise ValueError("unsupported datastream")
    method = runner.GetType().GetMethod(method_name)
    task = method.Invoke(runner, [subs, options, cancellation_none])

    def _wait_task():
        try:
            task.Wait()
        except Exception:
            pass

    thread = threading.Thread(target=_wait_task, daemon=True)
    thread.start()

    return {"runner": runner, "reader": reader, "task": task, "thread": thread}


def read_subscription_lines(handle: Dict[str, object], max_lines: int = 5) -> List[str]:
    """Read available JSONL lines from an active subscription."""
    reader = handle.get("reader")
    if reader is None:
        return []
    lines: List[str] = []
    try_read = reader.GetType().GetMethod("TryRead")
    if try_read is None:
        return lines
    while len(lines) < max_lines:
        args = System.Array[System.Object]([None])
        ok = try_read.Invoke(reader, args)
        if not ok:
            break
        lines.append(str(args[0]))
    return lines
