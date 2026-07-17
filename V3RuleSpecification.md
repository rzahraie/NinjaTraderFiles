# xPvaAutomatedContainerV3 Rule Specification

Status: Draft for chart-owner approval

Purpose: define V3 behavior before implementation. V1 and V2 are evidence sources only. No behavior becomes a V3 rule merely because earlier code implemented it.

## 1. Development Contract

- Implement one accepted behavior at a time.
- Compile and chart-test that behavior before beginning another.
- A stage passes only when its chart, NinjaScript log, and model/export data agree.
- Revert or correct a failed stage before proceeding.
- Never combine unrelated behavioral fixes in one test cycle.
- All containers remain visible after becoming adjusted, joined, frozen, inactive, or broken.
- Debug bounds limit processed bars but do not change model rules.

## 2. Vocabulary

- P1: container origin on the RTL side.
- P2: first-direction extreme on the opposite/LTL side.
- P3: later RTL support point.
- RTL: line through P1 and P3.
- LTL: line parallel to the RTL through P2.
- VE: parallel outer line created when price traverses the active LTL or VE.
- FTT: Failure To Traverse; the terminal extreme from which an opposite campaign may originate.
- Parent: structurally containing container.
- Child: container constructed within a live parent scope.
- Component: a container participating in a larger joined container.
- Active: eligible for extension and live structural decisions.
- Broken: terminated permanently; may contribute historical geometry but can never become live again.

## 3. Bar Relations

V3 consumes the existing discrete bar-relation classifications:

- HHHL
- LLLH
- HighReversal
- LowReversal
- OutsideBullish
- OutsideBearish
- InsideBar / SYM
- FTP
- Unknown

Classification must be deterministic and exported with each processed bar. V3 does not initially redefine the existing event engine's classifications.

## 4. Construction Rules

### 4.1 Up Container

- P1 is the origin bar low.
- A valid later RTL support low must be strictly above P1.
- Invariant: `P3 low > P1 low`.
- The initial LTL is parallel to the RTL through the applicable P2 high.
- Every intervening bar low must be checked. The RTL may not pass above an intervening low.

### 4.2 Down Container

- P1 is the origin bar high.
- A valid later RTL support high must be strictly below P1.
- Invariant: `P3 high < P1 high`.
- The initial LTL is parallel to the RTL through the applicable P2 low.
- Every intervening bar high must be checked. The RTL may not pass below an intervening high.

### 4.3 Outside Bars

- An Outside Bar may be the first/origin bar of a construction.
- An Outside Bar may not be the second bar of a two-bar construction.
- Extension or adjustment into a later Outside Bar is permitted only when all directional geometry remains valid.

### 4.4 Skipped and Synthetic Context

- IB/SYM and FTP bars may be skipped during construction.
- A pending or lateral origin may be consumed only once.
- Aggregate direction is determined from origin-to-end geometry when skipped bars separate the endpoints.
- If both Up and Down aggregate constructions are valid, V3 must not guess; the tie-breaking rule remains unresolved below.

## 5. Extension and Adjustment

- A live container may extend while its RTL has not validly broken.
- Same-direction bars extend an existing eligible container rather than creating a duplicate nested container.
- RTL wick adjustment is allowed only when the close remains inside and directional P1/P3 geometry remains valid.
- Adjustment cannot rotate an Up RTL below/equal to P1 or a Down RTL above/equal to P1.
- Adjustment changes geometry, not structural membership or lifecycle status.
- A Joined component remains Joined after geometry adjustment.

## 6. VE Rules

- Up: a high beyond the active LTL/VE creates the next VE.
- Down: a low beyond the active LTL/VE creates the next VE.
- The prior active outer line is frozen at the traversal bar.
- The new VE is parallel to the current RTL and anchored at the traversing extreme.
- Valid RTL recalculation rebuilds the complete LTL/VE history deterministically.
- VE processing is independent of whether a container will later be found structurally unnecessary.

## 7. Break and Lifecycle Rules

- Break detection precedes child creation and same-direction suppression.
- A broken container terminates exactly once.
- A broken container never extends, becomes Joined, or becomes Active again.
- A broken container may be retained as historical geometry in a later join.
- Breaking a child terminates that child and its dependent descendants.
- Breaking a parent deactivates its descendants except the explicitly promoted survivor.
- Containers remain rendered after termination.

The exact distinction between close break and full-bar break remains unresolved below.

## 8. Parent and Child Rules

- Ordinary child construction must be opposite the parent direction.
- No same-direction child is created merely because a same-direction transitional bar occurs.
- Exception: after a valid breakout of an opposite child, a same-direction response may be created.
- The response origin is the valid FTT, P3, pending, or lateral origin selected by the accepted origin-precedence rule.
- A child cannot remain live after any ancestor is broken unless it is the explicitly promoted survivor.
- Every live child has exactly one visual parent.

## 9. Promotion and Levels

- When a parent breaks, the qualifying surviving opposite-direction child is promoted.
- Promotion preserves container identity and geometry.
- Promotion removes the old parent scope and attaches the survivor to the old parent's parent.
- The promoted child receives the broken parent's level.
- Descendant levels are recalculated recursively.
- Level 1 is outermost; child level is always greater than parent level.
- Inactive historical containers retain their final recalculated style and remain visible.

## 10. Join Rules

### 10.1 Ordinary Triad

- Pattern must alternate: Up-Down-Up or Down-Up-Down.
- Components must be chronologically ordered.
- P1 geometry must be directionally valid.
- Joined parent P1/P3 geometry must satisfy the same strict Up/Down invariant.
- A join creates one larger container and demotes its components.
- Historical broken components remain Broken and cannot become terminal/live components.

### 10.2 Parent-Direction Restriction

- A joined child may not have the same direction as its live parent.
- Exception: the middle opposite component has validly broken, the left component is live, and the right response is live.
- The exception must not resurrect a broken left component.

### 10.3 Lineage and Point-3 Joins

- Valid Up sequence: Up1 P1 -> Down -> FTT/lowest qualifying point -> resumed Up.
- Valid Down sequence: Down1 P1 -> Up -> FTT/highest qualifying point -> resumed Down.
- A continuation may originate from an existing container's P3/FTT rather than its visible start bar.
- Mixed-level components may share structural lineage.
- Visual parent ownership and structural lineage are separate concepts.
- A historical component can contribute geometry without changing lifecycle status.

## 11. Rendering Invariants

- Up geometry is blue; Down geometry is red.
- Color always reflects model direction.
- Line style and width reflect final level.
- RTL, LTL, VE, and frozen VE segments use the owning container's final level.
- No RTL may pass through a bar that should have become an intervening P3 support.
- Rendering never mutates model state.

## 12. Determinism and Diagnostics

- Identical bars and settings produce identical IDs, hierarchy, geometry, events, and JSON.
- Every create, reject, extend, adjust, VE, break, promote, demote, join, and lineage decision has a reason.
- Decision logs include bar, container ID, direction, level, status, parent, origin, and relevant prices.
- Rejected alternatives must not mutate state.

## 13. ChatGPT JSON Contract

The exporter is developed alongside model behavior, not added after completion.

Required configuration:

- Enable/disable export.
- Export folder.
- Optional filename.
- Export on completion or every bar.
- Optional export diagnostics.

Required JSON sections:

- Metadata: indicator name/version, instrument, bars period, session template, timezone, processed bounds, timestamps.
- Bars: index, time, OHLCV, relation, transitional/translational facts, lateral context.
- Containers: identity, direction, level, lifecycle status, P1/P2/P3, RTL/LTL/VE, parent, children, lineage, reasons, styling.
- Bar membership: every container associated with each bar and its structural role.
- Events: ordered creation, adjustment, VE, break, promotion, demotion, join, and rejection events.
- Warnings: invalid geometry, missing references, cycles, duplicate ownership, status violations, and export inconsistencies.

Metadata indicator name for V3 must match the final NinjaScript class name exactly.

## 14. Mandatory Invariants

1. Up always has `P3 low > P1 low`.
2. Down always has `P3 high < P1 high`.
3. A broken container never becomes live again.
4. A live child never has a broken ancestor.
5. A child level is greater than its parent level.
6. One container has at most one visual parent.
7. Same-direction child creation requires an accepted opposite-breakout exception.
8. Pending/lateral origin is consumed at most once.
9. Join evaluation is atomic: accept fully or mutate nothing.
10. Model, rendering, log, and JSON describe the same state.

## 15. Unresolved Decisions

These questions block implementation beyond diagnostic scaffolding:

1. Break threshold: Is a close through RTL sufficient, or is a complete bar beyond RTL required? Which rule applies to parents, children, and joined containers?
2. Break bar endpoint: Does a container visually terminate on the breakout bar or the preceding bar?
3. Dual-valid aggregate geometry: When skipped bars make both Up and Down constructions valid, what selects direction?
4. Origin precedence: When FTT, P3, pending tape, lateral origin, and previous bar are all available, what is the exact priority order?
5. FTT confirmation: What exact event confirms an FTT, and can an FTT candidate move after a child is created?
6. Join traversal: Must the third component exceed the first component's P2 before joining, or is a broken opposite middle component sufficient?
7. Same-direction breakout response: Does it become a child of the existing parent, replace/extend that parent, or remain only a structural component?
8. Outside Bar extension: When an OB violates both RTL and LTL, which side has precedence and can it trigger both break and VE?
9. Joined terminal component: Must only the newest same-direction component extend with the parent, and does its break always terminate the joined parent?
10. Inactive levels: After later outer joins, should already broken historical containers be visually restyled to their new structural depth?
11. Class name: confirm `xPvaAutomatedContainerV3` (singular Container) versus `xPvaAutomatedContainersV3` (plural Containers).

## 16. Step 1 Acceptance Gate

Step 1 is complete only when:

- Sections 1-14 are approved or corrected.
- Every question in Section 15 is answered or explicitly deferred with a conservative V3 default.
- The final NinjaScript class/file name is confirmed.
- No V3 engine or rendering code has been written before this approval.
