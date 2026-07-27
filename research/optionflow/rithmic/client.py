"""Thin wrapper over async_rithmic for the OptionFlow sidecar.

Only the confirmed public surface of async_rithmic is used:

    RithmicClient(user, password, system_name, app_name, app_version, url, ...)
    await client.connect(plants=[...])
    await client.disconnect()
    await client.search_symbols(search_text, exchange=..., instrument_type=...)
    await client.get_front_month_contract(symbol, exchange)
    await client.subscribe_to_market_data(symbol, exchange, data_type)   # data_type: DataType|int
    client.on_tick  += handler(tick: dict)                               # tick keyed by protobuf field names

Two things we deliberately do NOT rely on:
  * async_rithmic decoding the open-interest message (template 158). We subscribe
    the OPEN_INTEREST update bit (value read from the proto enum at runtime, never
    hardcoded) and opportunistically read `open_interest` off any tick dict that
    carries it. If the installed library does not surface it, OI stays None and the
    collector marks coverage BLOCKED instead of inventing a number. Confirm this
    against your live feed; it is the one path that cannot be verified offline.
  * greeks from Rithmic. We imply vol from the option's own traded/mid price, so
    GEX is self-contained.
"""

from __future__ import annotations

import asyncio
from typing import Any, Callable

from .config import RithmicConfig


def _resolve_open_interest_bit() -> int | None:
    """Read the OPEN_INTEREST flag value straight from the generated proto enum,
    so we never guess the bit. Returns None if the enum member is absent."""
    try:
        from async_rithmic.protocol_buffers import request_market_data_update_pb2 as md
        return int(md.RequestMarketDataUpdate.UpdateBits.OPEN_INTEREST)
    except Exception:
        return None


def _silence_template158_warnings() -> None:
    """The ticker plant logs a WARNING for every template-158 frame because it has
    no handler for it. Our patch already captures those, so drop just that record
    to keep the console readable."""
    import logging

    class _Drop158(logging.Filter):
        def filter(self, record):
            return "template_id=158" not in record.getMessage()

    logging.getLogger("rithmic.plant.ticker").addFilter(_Drop158())


def _data_type_bits(*, quotes: bool = True, open_interest: bool = True) -> int:
    from async_rithmic import DataType
    bits = 0
    if quotes:
        bits |= int(DataType.LAST_TRADE) | int(DataType.BBO)
    if open_interest:
        oi = _resolve_open_interest_bit()
        if oi:
            bits |= oi
    return bits


class OptionFlowRithmic:
    def __init__(self, cfg: RithmicConfig):
        self.cfg = cfg
        self._client = None
        self._ticks: list[dict] = []
        self._tick_handlers: list[Callable[[dict], None]] = []
        self._quotes: dict[str, dict] = {}   # symbol -> {bid, ask, last, ts}
        self._depth_events: list[dict] = []  # MBO (depth-by-order) events
        self._book_events: list = []         # aggregated order-book events
        self._depth_reject: str | None = None

    # -- lifecycle ---------------------------------------------------------
    async def connect(self) -> None:
        from async_rithmic import RithmicClient
        from async_rithmic.enums import SysInfraType
        from . import patch158
        patch158.install()   # capture template-158 (open interest) instead of crashing
        self._client = RithmicClient(
            user=self.cfg.user,
            password=self.cfg.password,
            system_name=self.cfg.system_name,
            app_name=self.cfg.app_name,
            app_version=self.cfg.app_version,
            url=self.cfg.url,
        )
        self._client.on_tick += self._on_tick
        self._client.on_market_depth += self._on_market_depth   # MBO / depth-by-order
        self._client.on_order_book += self._on_order_book       # aggregated book
        _silence_template158_warnings()
        # Market data only. This is a data-only account: the PnL/Order/History
        # plants get "permission denied" (1011) and would abort the whole connect.
        # Subscribing just the ticker plant also means one login, not four —
        # fewer sessions to collide with "device already logged in" (1094).
        await self._client.connect(plants=[SysInfraType.TICKER_PLANT])

    async def disconnect(self) -> None:
        if self._client is not None:
            try:
                await self._client.disconnect()
            finally:
                self._client = None

    # -- tick fan-out ------------------------------------------------------
    def _on_tick(self, tick: dict, *_a, **_k) -> None:
        self._ticks.append(tick)
        sym = tick.get("symbol")
        if sym:
            q = self._quotes.setdefault(sym, {"bid": None, "ask": None, "last": None, "ts": None})
            if tick.get("bid_price") is not None:
                q["bid"] = tick["bid_price"]
            if tick.get("ask_price") is not None:
                q["ask"] = tick["ask_price"]
            if tick.get("trade_price") is not None:
                q["last"] = tick["trade_price"]
            q["ts"] = tick.get("ssboe") or q["ts"]
        for h in self._tick_handlers:
            h(tick)

    def latest_quotes(self) -> dict[str, dict]:
        return dict(self._quotes)

    # -- MBO / market depth ------------------------------------------------
    def _field(self, resp, name):
        try:
            return getattr(resp, name)
        except Exception:
            return None

    def _on_market_depth(self, resp, *_a, **_k) -> None:
        # DepthByOrder (template 160): order-level book updates.
        self._depth_events.append({
            "update_type": self._field(resp, "update_type"),
            "transaction_type": self._field(resp, "transaction_type"),
            "price": self._field(resp, "depth_price"),
            "size": self._field(resp, "depth_size"),
            "priority": self._field(resp, "depth_order_priority"),
            "order_id": self._field(resp, "exchange_order_id"),
            "seq": self._field(resp, "sequence_number"),
        })

    def _on_order_book(self, resp, *_a, **_k) -> None:
        self._book_events.append(resp)

    async def subscribe_market_depth(self, symbol: str, exchange: str, depth_price: float) -> None:
        await self._client.subscribe_to_market_depth(symbol, exchange, depth_price)

    def depth_events(self) -> list[dict]:
        return list(self._depth_events)

    def book_event_count(self) -> int:
        return len(self._book_events)

    def open_interest_by_symbol(self) -> dict[str, int]:
        """Decode captured template-158 frames into {symbol: open_interest}."""
        from . import patch158
        out: dict[str, int] = {}
        for fr in patch158.OI_FRAMES:
            d = patch158.extract_open_interest(fr)
            if d["symbol"] and d["open_interest"] is not None:
                out[d["symbol"]] = d["open_interest"]   # last one wins
        return out

    def mid_or_last(self, symbol: str) -> float | None:
        q = self._quotes.get(symbol)
        if not q:
            return None
        if q["bid"] is not None and q["ask"] is not None and q["ask"] >= q["bid"] > 0:
            return 0.5 * (q["bid"] + q["ask"])
        return q["last"]

    def add_tick_handler(self, fn: Callable[[dict], None]) -> None:
        self._tick_handlers.append(fn)

    # -- discovery ---------------------------------------------------------
    async def front_month_future(self, product: str, exchange: str) -> str:
        return await self._client.get_front_month_contract(product, exchange)

    async def search_option_symbols(self, product: str, exchange: str) -> list[dict]:
        """Return raw search results for a product's options. We parse the symbol
        string ourselves (contracts.parse_symbol), so we do not depend on the
        exact field names of the search response."""
        results = await self._client.search_symbols(product, exchange=exchange)
        # Normalise to list-of-dict regardless of the library's return container.
        out = []
        for r in results or []:
            if isinstance(r, dict):
                out.append(r)
            else:
                out.append({k: getattr(r, k) for k in dir(r)
                            if not k.startswith("_") and not callable(getattr(r, k))})
        return out

    # -- subscription ------------------------------------------------------
    async def subscribe(self, symbol: str, exchange: str, *, quotes=True, open_interest=True) -> None:
        bits = _data_type_bits(quotes=quotes, open_interest=open_interest)
        await self._client.subscribe_to_market_data(symbol, exchange, bits)

    async def collect_for(self, seconds: float) -> list[dict]:
        """Let ticks accumulate for a window, then return a snapshot copy."""
        await asyncio.sleep(seconds)
        return list(self._ticks)

    @property
    def open_interest_supported(self) -> bool:
        return _resolve_open_interest_bit() is not None
