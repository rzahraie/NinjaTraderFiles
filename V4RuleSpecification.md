# xPvaAutomatedContainerV4 Rule Specification

Status: Stage 14 - audit and export validation

V4 is developed as a deterministic replay engine. Each stage must compile and pass a chart/log acceptance test before another behavior is added. V1, V3, and TChannelV6 are evidence sources; none may mutate V4 behavior implicitly.

## 1. Stage Boundaries

1. Classification and ambiguous-formation state. (Accepted)
2. Provisional Up/Down geometry rendering. (Accepted)
3. Surviving-container commitment and continuation. (Accepted)
4. Lateral rectangle rendering and lifecycle. (Accepted)
5. Strict adjacent directional construction. (Accepted)
6. Ordinary directional lifecycle. (Accepted)
7. RTL invariant enforcement. (Accepted)
8. Skipped construction. (Accepted)
9. V1-compatible LTL and VE geometry. (Accepted)
10. Root/child hierarchy derivation. (Accepted)
11. Recursive hierarchy and promotion. (Accepted)
12. Ordinary triad joins. (Accepted)
13. Point-3, FTT, lineage, and mixed-level joins. (Accepted)
14. Export, invariant audit, and broad chart regression. (Current stage)

No later-stage rule may be implemented early to repair a current-stage test.

## 2. Immutable Input

- Every processed bar is captured once as index, time, OHLCV, and relation.
- Classification uses `xPvaDiscreteEventEngine` and is not altered by container state.
- Debug bounds change only the replay range, never the rules.
- Replaying identical bars and settings must produce identical events.

## 3. Ambiguous Formation

An IB/SYM or contained formation does not force a direction.

- Create provisional Up and Down candidates from the same origin.
- The Up candidate uses the origin low as P1 and a later higher low as P3.
- The Down candidate uses the origin high as P1 and a later lower high as P3.
- Both candidates retain the original formation origin while ambiguity remains.
- Both candidates may extend or adjust independently.
- While ambiguity remains, each candidate maintains an envelope RTL. Up moves P3 only
  when the existing RTL would pass above a later low; Down moves P3 only when the existing
  RTL would pass below a later high. P3 does not move merely because a newer bar exists.
- After lateral confirmation, a strict HHHL bar resolves the formation Up and discards
  Down; a strict LLLH bar resolves it Down and discards Up. Outside, stitch, inside-like,
  and reversal bars do not resolve direction by classification alone.
- Provisional candidates do not participate in hierarchy, levels, joins, promotion, VE, or committed-container suppression.
- A pending formation exclusively owns its replay sequence. No new top-level ambiguity may
  be seeded inside it. Nested laterals will be introduced later through explicit parent
  ownership, never by treating a pending parent as closed.
- If one candidate breaks, discard it and commit the surviving candidate without changing its identity or origin.
- The survivor continues through the resolving bar and subsequent bars.
- A committed survivor advances on every subsequent bar until broken.
- A wick may move the committed RTL envelope only when the close remains inside the
  existing RTL and the replacement P3 preserves strict directional geometry.
- A committed container breaks when the entire candle body is beyond its RTL. It remains
  visible as historical geometry through the break bar.
- If both candidates break on the same bar, commit neither and close the formation as failed.
- If neither breaks, the formation remains pending.

FTP and FBP are inside-like translational evidence. They may continue an existing ambiguous formation but do not independently force a direction.

## 4. Lateral Formation

- A confirmed lateral contains at least three bars.
- Bars 2 and 3 may equal but may not exceed bar 1's high or low.
- The origin bar defines the initial high and low boundaries.
- From bar 4 onward, a lateral is invalidated only when the entire candle body is beyond a boundary.
- Laterals may nest.
- A resolved directional container retains the lateral's original origin.
- Lateral rectangles use RenderTarget with a transparent fill.
- Lateral lifecycle is independent of provisional resolution and committed-container
  lifecycle. Resolving or breaking a directional candidate does not close its lateral.
- The rectangle includes the body-break bar and then freezes permanently.

Lateral confirmation and provisional directional resolution are related but separate facts. One must not overwrite the other.

## 5. Directional Invariants

- Up: `P3 low > P1 low`.
- Down: `P3 high < P1 high`.
- Horizontal geometry is never a committed directional container.
- An Outside Bar may be an origin but may not be the second bar of a two-bar committed construction.
- Rendering consumes model state and never mutates it.

## 6. Ordinary Adjacent Construction

- Construction uses exactly two adjacent bars in this stage.
- Up requires the second low to be strictly above the origin low and non-decreasing highs.
- Down requires the second high to be strictly below the origin high and non-increasing lows.
- Up RTL connects origin low to second-bar low; its initial LTL begins at the origin high.
- Down RTL connects origin high to second-bar high; its initial LTL begins at the origin low.
- Inside-like bars are owned by the ambiguity engine and do not create ordinary containers.
- An Outside Bar may be the origin but may not be the second construction bar.
- A bar currently owned by a pending or committed ambiguous formation cannot also create
  a same-direction ordinary container. A pending formation suppresses both directions;
  after resolution, valid opposite-direction child construction is allowed.
- An existing active ordinary container extends instead of permitting duplicate
  same-direction adjacent containers.
- Ordinary adjustment and full-body RTL break use the same rules as a committed survivor.
- If a wick traverses an ordinary RTL but adjustment is illegal because the close is
  outside or strict P1/P3 geometry would be lost, the container is geometrically
  invalidated and freezes at the preceding bar. It is not allowed to continue with an
  RTL passing through price.
- Hierarchy assignment and joining remain deferred.
- FTP, FBP, and strict inside-like bars may preserve the preceding bar as a latent ordinary
  origin. The next non-inside bar may construct from that origin if all intervening geometry
  remains valid.
- A same-direction response inside a committed ambiguous owner is allowed when an active
  opposite-direction ordinary container terminated on that same resolving bar.
- Ordinary Up P2 is the highest high from P1 forward; ordinary Down P2 is the lowest low.
  P2 is structural state and does not anchor the initial LTL.
- If the second or any later bar traverses the active LTL/VE, the active outer line freezes
  at that bar and a new VE begins at the traversing extreme. All outer segments use the
  current fitted RTL slope so an RTL adjustment deterministically re-projects the family.

## 7. Stage 14 Acceptance Gate

Stage 14 renders provisional and committed ambiguity geometry, confirmed lateral rectangles,
and ordinary directional containers with lifecycle behavior.

The log must correctly show:

- every processed bar and relation;
- ambiguity creation with one shared origin and two candidate IDs;
- pending continuation without origin drift;
- independent candidate adjustment/break evidence;
- exactly one resolution event when one candidate breaks;
- failure when both candidates break;
- strict-direction resolution after lateral confirmation;
- no hierarchy, level, join, promotion, or VE events;
- a pending formation renders both provisional parallelograms;
- a discarded candidate is no longer rendered;
- the committed survivor remains rendered from the shared original formation origin;
- the committed survivor advances after its resolution bar;
- extension does not move P3 unless an accepted envelope adjustment is required;
- a committed break is logged once and permanently stops continuation;
- no second top-level formation is created while the current formation is pending or its
  committed survivor remains active.
- a lateral rectangle begins at the shared formation origin after the third bar confirms it;
- its fill is transparent and its outline remains visible;
- directional resolution and break do not stop the lateral rectangle;
- the lateral closes exactly once when an entire later candle body leaves its boundaries;
- strict adjacent Up and Down containers are drawn outside ambiguous ownership;
- every accepted or rejected adjacent pair is logged with its reason;
- no same-direction ordinary duplicate is created within the active 1026 ambiguity sequence;
- no hierarchy, level, join, promotion, or VE behavior occurs;
- the valid 1039-1040 Down container is created inside the committed Up scope;
- the 1041-1042 Up container advances without duplicate Up containers at 1042 or 1043;
- ordinary containers adjust and terminate exactly once without reactivation;
- geometric invalidation of the 1036 Down releases bar 1039 as a valid new origin;
- the valid 1039-1040 Down container is no longer suppressed by invalid geometry;
- an Up container is constructed from latent origin 1037 across FTP bar 1038;
- the 1041 Up initial LTL begins at the 1041 high, as in V1;
- bar 1042 immediately creates a VE when its high traverses the initial LTL;
- later VE traversals freeze the prior segment and begin a new active VE;
- P2 updates independently and never silently relocates the initial LTL.
- the committed ambiguity survivor is the level-1 root;
- ordinary containers created while that root is live derive it as parent and render level 2;
- same-direction responses after an opposite child terminates inherit the same root scope;
- ordinary containers created after the root break remain level 1;
- hierarchy derivation does not alter geometry, lifecycle, P2/P3, LTL, or VE state.
- creation provenance distinguishes formation ownership, ordinary ownership, and
  same-level response inheritance after an opposite container terminates;
- an ordinary child has one parent and derives `level = parent level + 1`;
- a response after an opposite child terminates inherits that child’s parent and level;
- when a formation root breaks, the latest live opposite child is promoted to level 1;
- promotion preserves container identity, geometry, lifecycle, P2, LTL, and VE history.
- an ordinary join requires three start-ordered alternating components with equal level and
  identical derived parent scope;
- absent the accepted opposite-break exception, the right component must traverse beyond
  the first component’s P2 in the joined direction;
- join acceptance is atomic and creates a new parent while preserving component identities;
- the joined RTL is fitted to every intervening wick and must retain strict P1/P3 geometry;
- the joined LTL begins at the first component’s P2, and joined outer traversals create VEs;
- accepted components move one level below the joined parent; rejected triads mutate nothing.
- a response-source chain may establish common lineage even after promotion changes levels
  and visual parent scope;
- mixed-level lineage joins require alternating P1 geometry, middle FTT, the V1
  same-direction extreme test, and valid fitted joined P1/P3 geometry;
- the joined level and parent scope come from the shallowest lineage member;
- an ordinary same-level triad is never reconsidered as a lineage join.
- lineage anchored by an ambiguous formation begins with that committed formation as the
  left same-direction component; it may not discard the formation P1 and restart at the
  first ordinary response;
- subsequent opposite/response pairs fold into the same joined parent, refitting P3 from
  the original formation P1 each time;
- the original formation and all folded ordinary components remain visible one level below
  the joined parent.
- post-replay auditing is read-only and reports directional geometry, RTL traversal,
  hierarchy ownership, level, joined-component, and outer-anchor violations;
- identical replay state produces an identical deterministic fingerprint;
- optional JSON export defaults off and cannot influence model or rendering decisions.
