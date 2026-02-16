from __future__ import annotations
import tkinter as tk
from tkinter import ttk
from tkinter import scrolledtext

import execguard_lib as lib


class DemoUI:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("ExecGuard DLL Demo UI")
        self.root.geometry("520x360")

        self.exchange_var = tk.StringVar()
        self.symbol_var = tk.StringVar()
        self.datastream_var = tk.StringVar()
        self.side_var = tk.StringVar(value="buy")
        self.order_type_var = tk.StringVar(value="market")
        self.quantity_var = tk.StringVar(value="1.0")
        self._subscription = None

        self._build_ui()
        self._load_datastreams()

    def _build_ui(self) -> None:
        frame = ttk.Frame(self.root, padding=12)
        frame.pack(fill=tk.BOTH, expand=True)

        ttk.Label(frame, text="Exchange").grid(row=0, column=0, sticky="w")
        self.exchange_combo = ttk.Combobox(
            frame,
            textvariable=self.exchange_var,
            values=["hl", "okx", "bybit", "mexc"],
            state="readonly",
            width=22,
        )
        self.exchange_combo.grid(row=0, column=1, sticky="w")
        self.exchange_combo.bind("<<ComboboxSelected>>", self.on_exchange_change)

        ttk.Label(frame, text="Symbol").grid(row=1, column=0, sticky="w", pady=(8, 0))
        self.symbol_combo = ttk.Combobox(
            frame,
            textvariable=self.symbol_var,
            values=[],
            state="readonly",
            width=22,
        )
        self.symbol_combo.grid(row=1, column=1, sticky="w", pady=(8, 0))

        ttk.Label(frame, text="Datastream").grid(row=2, column=0, sticky="w", pady=(8, 0))
        self.datastream_combo = ttk.Combobox(
            frame,
            textvariable=self.datastream_var,
            values=[],
            state="readonly",
            width=22,
        )
        self.datastream_combo.grid(row=2, column=1, sticky="w", pady=(8, 0))

        ttk.Label(frame, text="Side").grid(row=3, column=0, sticky="w", pady=(8, 0))
        self.side_combo = ttk.Combobox(
            frame,
            textvariable=self.side_var,
            values=["buy", "sell"],
            state="readonly",
            width=22,
        )
        self.side_combo.grid(row=3, column=1, sticky="w", pady=(8, 0))

        ttk.Label(frame, text="Order Type").grid(row=4, column=0, sticky="w", pady=(8, 0))
        self.order_type_combo = ttk.Combobox(
            frame,
            textvariable=self.order_type_var,
            values=["market", "limit"],
            state="readonly",
            width=22,
        )
        self.order_type_combo.grid(row=4, column=1, sticky="w", pady=(8, 0))

        ttk.Label(frame, text="Quantity").grid(row=5, column=0, sticky="w", pady=(8, 0))
        self.quantity_entry = ttk.Entry(frame, textvariable=self.quantity_var, width=24)
        self.quantity_entry.grid(row=5, column=1, sticky="w", pady=(8, 0))

        self.send_button = ttk.Button(frame, text="Send Trade", command=self.on_send_trade)
        self.send_button.grid(row=6, column=0, columnspan=2, pady=(12, 4))

        self.subscribe_button = ttk.Button(frame, text="Subscribe", command=self.on_subscribe)
        self.subscribe_button.grid(row=7, column=0, columnspan=2, pady=(0, 8))

        self.status = scrolledtext.ScrolledText(frame, height=8, wrap=tk.WORD)
        self.status.grid(row=8, column=0, columnspan=2, sticky="nsew")

        frame.columnconfigure(1, weight=1)
        frame.rowconfigure(8, weight=1)

    def _load_datastreams(self) -> None:
        datastreams = lib.get_datastreams()
        self.datastream_combo["values"] = datastreams
        if datastreams:
            self.datastream_var.set(datastreams[0])

    def on_exchange_change(self, _event=None) -> None:
        exchange = self.exchange_var.get()
        if not exchange:
            return

        connected = lib.connect(exchange)
        if not connected:
            self._log(f"Error: failed to connect to {exchange}")
            return

        symbols = lib.get_symbols(exchange)
        self.symbol_combo["values"] = symbols
        if symbols:
            self.symbol_var.set(symbols[0])
        else:
            self.symbol_var.set("")

        self._log(f"Connected to {exchange}. Loaded {len(symbols)} symbols.")
        if not symbols:
            self._log("No symbols available for this exchange in the demo.")

    def on_send_trade(self) -> None:
        exchange = self.exchange_var.get().strip()
        symbol = self.symbol_var.get().strip()
        datastream = self.datastream_var.get().strip()
        side = self.side_var.get().strip()
        order_type = self.order_type_var.get().strip()
        quantity_text = self.quantity_var.get().strip()

        if not exchange or not symbol or not datastream:
            self._log("Error: select exchange, symbol, and datastream.")
            return

        try:
            quantity = float(quantity_text)
        except ValueError:
            self._log("Error: quantity must be a number.")
            return

        result = lib.send_order(exchange, symbol, datastream, side, order_type, quantity)
        if not result.get("success"):
            self._log(f"Error: {result.get('error', 'order failed')}")
            return

        self._log(
            "Order submitted: "
            f"id={result['order_id']} "
            f"exchange={result['exchange']} "
            f"symbol={result['symbol']} "
            f"side={result['side']} "
            f"type={result['order_type']} "
            f"qty={result['quantity']}"
        )

    def on_subscribe(self) -> None:
        exchange = self.exchange_var.get().strip()
        symbol = self.symbol_var.get().strip()
        datastream = self.datastream_var.get().strip()

        if not exchange or not symbol or not datastream:
            self._log("Error: select exchange, symbol, and datastream.")
            return

        try:
            self._subscription = lib.start_subscription(
                exchange,
                symbol,
                datastream,
                max_messages=20,
                timeout_seconds=20,
            )
        except Exception as exc:
            self._log(f"Error: subscribe failed: {exc}")
            return

        self._log(f"Subscribed to {exchange} {datastream} for {symbol}.")
        self._poll_subscription()

    def _poll_subscription(self) -> None:
        if self._subscription is None:
            return

        try:
            lines = lib.read_subscription_lines(self._subscription, max_lines=10)
        except Exception as exc:
            self._log(f"Error: reading stream failed: {exc}")
            self._subscription = None
            return

        for line in lines:
            self._log(line)

        if self._subscription is not None:
            self.root.after(500, self._poll_subscription)

    def _log(self, message: str) -> None:
        self.status.insert(tk.END, message + "\n")
        self.status.see(tk.END)


if __name__ == "__main__":
    root = tk.Tk()
    app = DemoUI(root)
    root.mainloop()
