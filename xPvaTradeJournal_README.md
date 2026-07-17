# xPvaTradeJournal

## Files

- `xPvaTradeJournal.cs` - chart indicator, status panel, APVA context provider.
- `xPvaTradeJournalService.cs` - singleton account subscription service, event queue, campaign reconstruction.
- `xPvaTradeJournalDatabase.cs` - SQLite schema and parameterized persistence.
- `xPvaTradeJournalModels.cs` - enums, DTOs, campaign state, context-provider interface.

## Database

Default SQLite path:

`Documents\NinjaTrader 8\apva\journal\xPvaTradeJournal.sqlite`

The service enables WAL mode when supported and creates:

- `SchemaVersion`
- `JournalSession`
- `OrderEvent`
- `Execution`
- `PositionEvent`
- `TradeCampaign`
- `CampaignLeg`
- `ContextSnapshot`
- `ReasonTag`
- `SystemEvent`

Emergency fallback path:

`Documents\NinjaTrader 8\apva\journal\emergency\xPvaTradeJournal_EMERGENCY_YYYYMMDD.jsonl`

Daily summary path:

`Documents\NinjaTrader 8\apva\journal\daily\xPvaTradeJournal_YYYYMMDD.csv`

## Required Reference

NinjaTrader 8.1.7.2 includes:

`C:\Program Files\NinjaTrader 8\bin\System.Data.SQLite.dll`

If NinjaScript compilation reports that `System.Data.SQLite` is missing, add that DLL as a NinjaScript reference.

## Installation

1. Copy the four `.cs` files into:
   `Documents\NinjaTrader 8\bin\Custom\Indicators`
2. Compile NinjaScript.
3. Add `xPvaTradeJournal` to one chart.
4. Set `Account name`, default `Sim101`.
5. Leave `Show status panel = true` for first validation.

## Matching

Campaigns are separated by account and instrument.

- flat to non-flat opens a campaign;
- same-direction execution increases the campaign;
- opposite execution reduces/closes the campaign;
- reversal closes the old campaign and opens a new one for the remainder.

P&L is calculated from execution price, quantity, campaign direction, and `Instrument.MasterInstrument.PointValue`.

## Context

Execution persistence is mandatory. Context is best effort.

The chart indicator registers as a context provider and snapshots:

- recent price bars;
- latest `xPvaOrderFlowCore` registry bar when available;
- latest `xPvaOrderFlowFeatures` registry row when available.

`xPvaOrderFlowEvidence` is marked unavailable in this first build because it has no shared registry.

## Reset Protection

The journal database is independent of NinjaTrader's simulation database. Completed campaigns remain in SQLite and the daily CSV after a Sim101 reset. Startup open positions with unavailable execution history are marked `Reconstructed`.

## Validation

Use the known NQ examples:

- July 15, 2026 gross: `10055`
- July 16, 2026 gross: `3815`
- Combined gross: `13870`

Also test:

- simple short;
- reversal;
- scale-in;
- scale-out;
- partial fills;
- restart with open position;
- context source unavailable;
- database locked/emergency JSONL fallback;
- interleaved NQ/ES trades.

## Known Limitations

- Source attribution is conservative and remains `Unknown` unless reliable.
- Hotkey tagging is not implemented in this first build; service-level campaign tagging is present for future UI wiring.
- `CaptureAllAccounts` is exposed but the first implementation subscribes to the selected account only.
- Evidence snapshots await a shared evidence registry.
