# APVA V2 Development Plan

Purpose: keep V2 work batched, testable, and cheap enough to continue without losing the domain rules already discovered.

## Working Rules

- V1 `xPvaAutomatedContainers.cs` is frozen unless explicitly requested.
- V2 work stays in:
  - `xPvaContainerModelV2.cs` for model, rules, reducer, planner, engine, and self-tests.
  - `xPvaAutomatedContainersV2.cs` for NinjaTrader input/drawing adapter.
  - `xPvaV2SelfTest.cs` for the NT self-test runner.
- Prefer fixture-backed changes over speculative hardening.
- Batch related changes and compile once per batch.
- Compile immediately only for public API changes, NT wrapper changes, or rendering changes.
- Every completed code batch must update self-test count, pass compile, remove `_codex_xpva_compile_check.dll`, and leave only expected V2 files in `git status --short`.

## Batch Queue

### 1. Engine Lifecycle And P3 Join Correctness

Goal: validate the full flow around breakout, freeze/deactivation, promotion, extension fallback, P3 continuation, and mixed-level join parents.

Focus fixtures:
- `740 -> 781` P3-origin continuation.
- `793 -> 808 -> 825` mixed-level down/up/down join.
- `810 -> 850` joined level-1 container behavior.
- `824` breakout promotes existing child, does not create same-direction child.
- `831` breakout freezes old side and promotes existing opposite child.

Acceptance:
- No same-direction child is created inside a parent except after valid opposite breakout context.
- P3-origin joins may cross visual/structural levels.
- Join parent chooses the outer structural container and marks components joined.
- Joined/frozen/inactive components do not participate in new joins.

Current coverage:
- 740 -> 781 P3-origin continuation is covered by planner and sequential processing fixtures.
- 793 -> 808 -> 825 -> 850 mixed-level join is covered, including outer level-1 parent creation and component retirement from live selection.
- 824 and 831 breakout/promotion fixtures are covered.

### 2. Construction And Extension Rule Fixtures

Goal: pin bar-relation construction rules that caused visual/log defects.

Focus fixtures:
- No two-bar construction when the second bar is OB.
- OB may be first bar.
- IB/SYM bars adjust/extend valid existing containers rather than creating malformed containers.
- UP requires `P3(low) > P1(low)`.
- DOWN requires `P3(high) < P1(high)`.

Acceptance:
- No malformed blue/red crossing containers.
- Rejected construction paths are trace-visible.

Current coverage:
- Second-bar OB rejection is covered.
- First-bar OB construction is covered for both Up and Down when the second bar supplies valid direction and P1/P3 geometry.
- IB/SYM extension of an existing valid container is covered without P3 mutation.
- UP/DOWN P1/P3 construction geometry is covered.
- 805-808 and 827-829 malformed construction/extension fixtures are covered.

### 3. Reducer Atomicity And Idempotence Audit

Goal: ensure commands are either fully applied once or rejected without mutation.

Covered so far:
- Duplicate create.
- Duplicate join parent.
- Duplicate relationship links.
- Duplicate status/freeze/promotion.
- Invalid/shortening spans.
- Promotion idempotence is checked before parent containment links are created.

Remaining audit:
- Composite join partial mutation is deferred unless relationship rules begin depending on newly-created join parent state. Current reducer prevalidates all current rejection paths before creating the parent.
- Extension with P3 adjustment is covered as a rejected invalid/shortening span.

### 4. Rendering Snapshot Contract

Goal: guarantee all containers remain visible, regardless of active/inactive status.

Acceptance:
- Active, adjusted, frozen, joined, broken, and structurally deactivated containers appear in render snapshot.
- Component containers remain visible after join.
- Visual style reflects status without hiding geometry.

Current coverage:
- Render snapshot includes RTL/LTL segments for every container status.
- Joined components and active join parents remain visible together.

### 5. NT8 Adapter Wiring

Goal: keep NinjaTrader-specific code thin and predictable.

Acceptance:
- Bar relation translation is isolated.
- Debug range controls only input scope, not model behavior.
- Drawing tags are deterministic.
- Self-test indicator remains available.

Current coverage:
- NT8 bar-relation translation is isolated in `xPvaV2Nt8Adapter` and covered for every discrete relation.
- Debug/live input bounds are covered as adapter scope decisions.
- Render segment tag generation is deterministic.
- NT8 command/trace replay export summaries match the model-only replay format.
- NT8 debug output emits paste-ready compact fixture rows for exact evidence capture.

### 6. Real Log Replay Harness

Goal: reduce image/log debugging cost by replaying committed evidence directly.

Acceptance:
- Parse a compact bar fixture format.
- Run model-only replay without NT.
- Emit command/trace summaries around requested bar ranges.

Current coverage:
- Compact `bar,high,low,relation` fixture rows parse with comments and whitespace.
- Malformed compact rows and unknown relations reject visibly.
- Named evidence fixture catalog exists for `805-808`, `824-831`, and `793-850`.
- Evidence fixture catalog exposes keyed window metadata for deterministic lookup/replacement.
- Catalog rows are checked for chronological order and replay output.
- Catalog rows are checked for historical anchor bars and exact replay-range filtering.
- Model-only replay runs sequential bars without chart dependencies.
- Replay reports filtered command and trace summaries for requested bar ranges.
- Replay summaries are formatted through the same NT8 adapter export methods used by chart-side debug output.
- Chart-side fixture rows round-trip through the model-only replay parser.
- Raw NT8 debug output can be parsed for fixture rows while ignoring trace/command/preview noise.
- Raw NT8 debug output can build filtered named window fixtures for exact historical ranges.
- NT8 debug output brackets fixture rows with begin/end markers for easier copying.
- Marked fixture blocks can be extracted by range from pasted output containing multiple builds.
- Marked fixture extraction rejects copied blocks that are missing the end marker.
- Marked fixture blocks can be extracted by keyed catalog window.
- Keyed fixture replacement can be previewed against current catalog text before editing constants.
- Replacement previews report match status plus generated/catalog row counts.
- Replacement preview summaries can be emitted in catalog order for pasted debug captures.
- V2 debug builds print fixture preview summaries after the marked fixture block.
- Replacement previews fall back to full-range fixture rows when catalog-specific markers are absent.
- V2 debug build summary reports both self-test failures and expected check count.
- Keyed catalog windows may include setup bars outside the display key when needed for replay context.

## Next Phase Recommendation

Move from framework contracts to evidence fixtures:

1. Replace the current compact fixture skeletons with exact rows generated by keyed `BuildMarkedWindowFixtureFromDebugOutput` as each window is revisited.
2. Add behavior assertions to the named replay windows before changing algorithm rules.
3. Use the NT8 replay export summaries to compare chart-side debug output against model-only evidence replay.
4. Promote only fixture-backed behavior into the NT8 adapter and chart comparison pass.
