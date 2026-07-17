# xPvaDrawingRecorder

`xPvaDrawingRecorder` records the lifecycle of APVA chart drawings without manual export or drawing IDs. It records objects only; it does not reconstruct, pair, classify, or discover containers.

## Source Files

- `xPvaDrawingRecorder.cs` - chart adapter, UI-thread snapshot capture, controlled scanning, debounce, deletion detection, and status panel.
- `xPvaDrawingRecorderService.cs` - stable identity, revisions, ordered persistence, JSON snapshots, emergency fallback, and registry queries.
- `xPvaDrawingRecorderModels.cs` - immutable DTOs and enums.
- `xPvaDrawingRecorderDatabase.cs` - reflected SQLite provider, schema, WAL configuration, and transactional event/current writes.
- `xPvaDrawingRegistry.cs` - read-only API used by `xPvaTradeJournal` and later APVA components.

## Installation

1. Keep all five source files in `Documents\NinjaTrader 8\bin\Custom\Indicators`.
2. Compile NinjaScript.
3. Add `xPvaDrawingRecorder` once to each chart whose drawings should be recorded.
4. Leave `CaptureAllDrawingTools` false to capture only `CustomLine` and JH `xManualContainer` objects.

No additional NinjaScript assembly reference is required. The database layer loads NinjaTrader's installed `System.Data.SQLite.dll` at runtime, avoiding a compile-time `System.Data` dependency.

## Default Paths

- SQLite: `Documents\NinjaTrader 8\apva\drawings\xPvaDrawingRecorder.sqlite`
- JSON snapshots: `Documents\NinjaTrader 8\apva\drawings\sessions\`
- Emergency JSONL: `Documents\NinjaTrader 8\apva\drawings\emergency\`

SQLite uses WAL journaling and full synchronous writes. `DrawingEvent` is append-only and authoritative. `DrawingCurrent` is updated in the same transaction as each event.

## Supported Drawings

The default recorder recognizes:

- `CustomLine`
- JH Containers (`xManualContainer`)

JH Containers receive first-class handling. Their XML-persisted `ContainerId` is used as a high-confidence source identity, `GeometryChanged` is subscribed on the UI thread, and component role, level, and Gaussian inclusion are retained as raw source metadata. The recorder does not use JH parent IDs, component sequences, or Gaussian metadata to infer relationships.

Set `CaptureAllDrawingTools` to true only when unrelated NinjaTrader drawing tools should also be recorded.

## Revision And Debounce Behavior

The chart adapter inspects drawing objects on the UI thread and immediately copies their values into DTOs. No WPF or chart reference enters the persistence queue.

The adapter scans on a controlled dispatcher timer, not on market-data events. A changed snapshot must remain stable for `ModificationDebounceMilliseconds` (default 500 ms) before one revision is written. JH geometry notifications can occur repeatedly during a drag, so they do not bypass this stability rule.

Two consecutive scans must fail to find a previously tracked drawing before `Deleted` is emitted. Stopping the indicator, closing a chart, unloading a workspace, or recompiling NinjaScript stops the timer and never emits deletion.

## Reconciliation

Identity matching uses this conservative order:

1. JH `ContainerId` plus instrument.
2. Chart identity, native tag, and drawing-tool type.
3. A unique match on instrument, drawing type, anchor timestamps, and prices.

Workspace-restored drawings do not receive duplicate `Created` events. Changed bar-index resolution produces `Reconciled`; unmatched persisted active objects on the same chart identity become `Missing`, never `Deleted`.

Chart identity combines the strongest values exposed to an indicator: workspace identity when available, chart-window name/title, chart-control name, instrument, BarsPeriod type/value, Trading Hours template, and panel index. NinjaTrader does not expose a documented persistent chart-tab GUID to NinjaScript, so two identically configured unnamed tabs can remain ambiguous. JH `ContainerId` is not affected by this limitation.

## Semantic Metadata And Existing Hotkeys

No new hard-coded shortcuts are installed. This avoids collisions with NinjaTrader and user bindings. Existing `CustomLine` commands continue unchanged and their resulting revisions are recorded:

- `Ctrl+Q` cycles dash style.
- `+` and `-` adjust width.
- `Ctrl+R` extends the line/container.

The recorder reads explicit `xPvaRole`, `LineRole`, `xPvaStructuralLevel`, or `StructuralLevel` properties when a supported drawing exposes them. It also recognizes explicit native-tag prefixes `RTL-`, `RTL_`, `LTL-`, `LTL_`, `VE-`, and `VE_`. It does not infer semantics from color, width, or dash style.

## Registry And Trade Journal

Read-only access is available through:

```csharp
xPvaDrawingRegistry.TryGetActiveDrawings(instrument, timestampUtc, out drawings);
xPvaDrawingRegistry.TryGetDrawingHistory(drawingId, out events);
xPvaDrawingRegistry.TryGetChartDrawingState(chartIdentity, out state);
```

Timestamp queries choose the latest known revision at or before the requested UTC time. `xPvaTradeJournal` now includes this drawing state in execution context JSON. A missing recorder or unavailable drawing state does not stop trade recording.

## SQLite Schema

Schema version 1 creates:

- `SchemaVersion`
- `RecorderSession`
- `ChartContext`
- `DrawingEvent`
- `DrawingCurrent`
- `RecorderSystemEvent`

`DrawingEvent` has unique constraints on `(DrawingId, Revision)` and `SequenceNumber`. Writes use parameters only. Event insertion and current-state materialization share one transaction.

## JSON Schema

Session JSON uses `schemaVersion: 1` and contains chart context, `activeDrawings`, `reconciledDrawings`, `missingDrawings`, `deletedDrawings`, and ordered events. Each drawing contains identity, revision, status, semantics, anchors, style, timestamps, and relationship placeholders. Files are regenerated through a temporary file and atomic replacement.

## Validation

1. Add the recorder to an NQ or ES chart and confirm `Database: Connected`.
2. Draw one `CustomLine` or JH Container. Confirm one `Created` event and revision 1.
3. Move an anchor repeatedly, release it, and wait 500 ms. Confirm one final `AnchorChanged` revision.
4. Use the existing style and extension commands. Confirm `StyleChanged` or `ExtensionChanged` revisions.
5. Delete the drawing. Confirm `Deleted` while prior history remains.
6. Reload NinjaScript and the workspace. Confirm the drawing keeps its ID and no duplicate `Created` event appears.
7. Close a chart without deleting its drawings. Confirm no `Deleted` event is added.
8. Make the SQLite path unavailable and confirm valid independent emergency JSONL records and a visible status error.

## Known Limitations

- NinjaTrader does not provide a documented persistent chart-tab GUID through the supported indicator API.
- `CustomLine` currently exposes no persistent explicit APVA role property; role remains `Unknown` unless an explicit recognized tag/property exists.
- Explicit deletion is determined by disappearance from a still-running chart after two scans. Chart disposal is handled separately and never records deletion.
- Emergency events are retained in memory and retried with database uniqueness constraints providing deduplication. JSONL remains as an audit trail.
