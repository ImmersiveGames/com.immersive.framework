# IF-ADR-049 Amendment 001 — Scoped On-Demand Actor Occurrence Query

Status: **Proposed — recommended for acceptance by the Project Owner**
Proposed: **2026-10-31**
Amends: [IF-ADR-049 — Public Actor Occurrence Reference and Resolution](IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md) §5 and §9 (clarification and additive consumer role)
Related decisions: [IF-ADR-040](IF-ADR-040-Scene-Composition-Binding-Model.md), [IF-ADR-050](IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md) and its [accepted-target migration contract](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md) (M03/M04), [IF-ADR-051](IF-ADR-051-Actor-Groups-Identity-Membership-and-Player-Slot-Associations.md)
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

This amendment is a proposal. It is not normative until the Project Owner accepts it. It authorizes no Runtime change, API signature, implementation plan, consumer edit, maturity change or release.

## 1. Context

### 1.1 Current implemented contract

The audited source is `1.2.0-preview.4` at `976abd1` (`main`). The public consumer surface of IF-ADR-049 consists of value types (`ActorOccurrenceReference`, `ActorOccurrenceResolution`, `ActorOccurrence`, `ActorOccurrenceId`) and one access point to the resolver: `ActorOccurrenceReferenceBinding`.

The resolver itself, `ActorOccurrenceReferenceRuntime`, is `internal`. It is owned by `FrameworkRuntimeHost` and also holds producer authority: owner staging/commit/rollback/release, dynamic publication, the internal Player source (`IPlayerPreparedActorOccurrenceSource`), the Session owner and consumer refresh. Keeping that class internal is correct and remains required by IF-ADR-049 §2, §8 and §10 and by IF-ADR-040.

`ActorOccurrenceReferenceBinding` combines three roles:

| Role | Members | Nature |
|---|---|---|
| Scope anchor | `BindRuntime` / `UnbindRuntime` through `ActorOccurrenceReferenceSceneLifecycleParticipant`; `Unavailable` while unbound; `BindingReleased` on scope release | Essential to legitimate resolution |
| Scene authoring | Serialized Player Slot / `ActorDeclaration` / Session, Route or Activity owner selectors; `OnValidate` | Specific to scene authoring |
| Continuous observation of **one** selector | `_runtimeOverride`, `_current`, `UpdateCurrent`, `Changed` (`Available`, `Replaced`, `Unavailable`, `Invalidated`, `BindingReleased`), refresh from `RefreshAll` | Specific to observation |

### 1.2 Gap

A consumer whose targets are only known at runtime (for example the `ActorId`s returned by an Actor Group, or Actors named by evidence held by a gameplay system) has no legitimate public way to resolve several occurrences on demand:

- Retargeting one binding with `TrySetReference` mutates shared observation state and dispatches `Changed` events that describe no real occurrence change to every subscriber.
- One binding per target is not authorable when the target set is dynamic, and bindings are discovered only in `OnSceneAvailable` for already composed roots; a binding instantiated later never receives the runtime.
- Retaining an `ActorRoot` from an earlier result risks a stale object after replacement or release.

IF-ADR-049 §9 already requires that "the same reference semantics must be usable from ordinary C# consumers without requiring all gameplay systems to inherit from a specific MonoBehaviour". The current public documentation satisfies this only by letting C# code use a binding instance. The gap is therefore a missing consumer role of the accepted contract, not a missing resolver.

### 1.3 Reference use cases

The decision must serve both cases with one generic contract and no HUD- or knowledge-specific mechanism:

1. **Continuous observation.** A HUD follows the current Player of one Slot and reacts when its occurrence is replaced or becomes unavailable.
2. **Point-in-time query.** A knowledge system that already holds a legitimate identity and evidence must, at a moment its own policy authorizes, read the current occurrence once to capture an authorized location.

## 2. Decision (proposed)

### 2.1 Two consumer roles over one resolver

The IF-ADR-049 contract has two public consumer roles:

| | Observation | Query |
|---|---|---|
| Purpose | Follow one selector over time | Resolve any valid selector at one moment |
| Surface | `ActorOccurrenceReferenceBinding` (unchanged) | A read-only query capability defined by this amendment |
| State | One selector, last result | None |
| Notifications | `Changed` | None |
| Typical consumer | HUD, follow target, authored target | Systems that receive Actor identities at runtime |

Both roles delegate to the same `ActorOccurrenceReferenceRuntime.Resolve` of the same host. The query role introduces no registry, cache, index, enumeration, second resolver or second publication path. It uses the existing selector type, the existing `ActorOccurrenceResolution` result and the existing status vocabulary (`Resolved`, `Unavailable`, `InvalidReference`, `Stale`, `Ambiguous`).

The binding is not deprecated, replaced or changed. It remains the canonical surface for authored references and continuous observation.

### 2.2 Legitimate access through scoped composition

1. The query capability is obtained only through a scope anchor delivered by the IF-ADR-040 composition model: a component beneath roots explicitly composed for a committed Session, Route or Activity owner, bound on `Available`/owner commit and unbound on `Releasing`/owner release, in the established `ActorGroupAccessBinding` and `PlayerActorCompositionAccess` pattern. Binding uses the existing Actor occurrence composition participant, not a new binder or discovery pass.
2. The anchor exposes the capability as a public, read-only C# interface. Ordinary C# objects receive that interface by explicit hand-off from the component or system that owns them. They never locate it through a static member, service locator, global scan, `FindObjectOfType`, host lookup or singleton.
3. Transient owners are not supported, as in IF-ADR-051.
4. The anchor owner defines the capability's **lifetime only**. It does not restrict which committed occurrences can be resolved, does not grant gameplay authorization and does not partition the host resolver. Owner selection within a query remains expressed by the selector, exactly as today.
5. The interface exposes resolution only. It never exposes the runtime, publication, commit, release, the internal Player source or materialization handles (IF-ADR-049 §10 unchanged).

### 2.3 Query semantics

1. **Side-effect free.** A query does not change any selector, binding, consumer list, publication, occurrence identity or result cache and does not dispatch any notification.
2. **Reentrant.** A query is valid inside a `Changed` callback or during a host refresh, and observes the committed state at that moment.
3. **Committed state only.** Staged owners, pre-commit candidates and intermediate materialization are never returned (IF-ADR-049 §5.3 and §7; IF-ADR-050 §3).
4. **Ambiguity and staleness.** Ambiguous selectors return `Ambiguous`, never a chosen candidate. Exact-occurrence selectors return `Stale` after replacement or release.
5. **Lifetime.** Before bind and after release every query returns `Unavailable` with a diagnostic. A query after release never throws, never reaches a released or shut-down host and never returns retained evidence.
6. **Point-in-time evidence.** A successful result describes the occurrence at the moment of the query. Consumers that need durable data capture values (for example a position) together with the `ActorOccurrenceId`; they must not treat the `ActorRoot` as current beyond their own operation without querying again or using an exact-occurrence selector.
7. **Cost is documented.** Resolution is proportional to current publications and physical Player occurrences. The query role is meant for event- or decision-driven reads. Continuous tracking uses observation; per-frame multi-target polling is outside the intended use.

### 2.4 Physical resolution is not authorization

A successful query states only that a valid physical occurrence exists (IF-ADR-049 §6). It does not grant participation, readiness, input, perception, targeting, knowledge legitimacy or authority to move or modify the Actor. Whether a consumer may query, what it may keep and what it may infer remains the consumer's own policy. The Framework does not check gameplay authorization on queries.

### 2.5 Forward compatibility with IF-ADR-050

Query reports physical availability exactly as the resolver does today. During a guarded A→B handoff, a physical query may return `Unavailable` while A is still the committed logical identity; this is an expected transient outcome and must not be documented as "Actor left". The query role must not become a back door for the IF-ADR-050 M04 committed-binding view. When that typed view is introduced by its own amendment, it applies equally to observation and query.

### 2.6 Maturity

The query capability is a new Experimental consumer contract under IF-GOV-001. No Stable contract changes.

## 3. Application to the reference use cases

**HUD (observation).** The HUD uses `ActorOccurrenceReferenceBinding` with a Player Slot selector. It reads `Current` on enable and reacts to `Changed`: `Replaced` rebinds presentation to the new root; `Unavailable` hides or degrades it; `BindingReleased` stops observation. Nothing in this amendment changes that flow.

**Knowledge (query).** The knowledge system receives the query interface from the composed component that owns it. When its own policy accepts a legitimate evidence event, it queries once, preferably with an exact-occurrence selector built from the evidence (`ForOccurrence`) or with `ForActorId` plus the exact owner. On `Resolved`, it captures the authorized value (for example the root position), the `ActorOccurrenceId` and its own timestamp. On `Stale`, `Unavailable` or `Ambiguous`, it records no location. It does not poll and does not retain the root.

Both cases use only generic Framework contracts.

## 4. Conflicts with prior decisions and their resolution

| Decision | Potential conflict | Resolution in this amendment |
|---|---|---|
| IF-ADR-049 §9 | §9 lists the authoring binding's responsibilities and requires ordinary C# use; current documentation satisfies C# use through the component | Clarifies that ordinary C# use is satisfied by the scoped query interface; the binding's responsibilities are unchanged. Public API documentation must be updated at implementation |
| IF-ADR-049 §2, §8, §10 | Any public resolver risks global access or leaked producer authority | Query is host-scoped through composition, read-only, and exposes no internal type |
| IF-ADR-040 | A new access path could become a service locator or second scanner | Capability arrives only through the existing composition participant and explicit roots; plain C# receives it by hand-off |
| IF-ADR-050 M03/M04 | Query could reinterpret physical `Unavailable` or expose candidates | Query keeps current physical semantics; committed-binding view deferred to its own amendment and applied to both roles |
| IF-ADR-051 | Coupling Groups to occurrences | Not done. Membership stays by `ActorId`; Group consumers resolve occurrences through the generic query role |
| Existing binding discovery | Consumers created after `OnSceneAvailable` are unbound | Not changed. A runtime-created C# consumer obtains the capability by hand-off from a composed anchor, without new discovery |

No accepted decision is superseded. The original IF-ADR-049 text and history remain intact; this amendment supplements §5 and §9.

## 5. Rejected alternatives

- **Making `ActorOccurrenceReferenceRuntime` public or reachable from the host.** Exposes producer authority and the internal Player source; violates IF-ADR-049 §2/§8/§10 and IF-ADR-040.
- **Static, singleton or global resolver.** Service locator; violates IF-ADR-049 §8 and IF-ADR-040.
- **Retargeting a binding for each query.** Corrupts observation state and emits false notifications.
- **One binding per target as the required pattern.** Not authorable for dynamic sets and does not bind runtime-created consumers.
- **Adding a stateless query method to the binding.** Smaller change, but mixes authoring/observation with an unrelated capability and forces consumers to carry an unused selector. Kept only as a fallback if the Project Owner rejects a new access type.
- **Enumerating all current occurrences.** Equivalent to a global scan; outside this decision.
- **HUD- or knowledge-specific APIs.** Specializations are consumer responsibilities.
- **Multi-target observation subscriptions.** Larger contract with subscription disposal and refresh-cost risks; deferred until a consumer demonstrates the need.

## 6. Conditions before implementation

1. The Project Owner accepts this amendment; IF-ADR-049 and the tracker record the acceptance.
2. An architecture review using the installed `immersive-framework-architecture-audit` and `immersive-framework-core-architecture` skills confirms this text against the exact source revision. They were not available in the session that drafted this proposal.
3. A separate implementation plan is approved under the Framework plan process. It defines the smallest public names and signatures, reuses the existing participant and resolver, and adds no storage.
4. The plan covers at least: query before bind and after release returns `Unavailable`; a query dispatches no `Changed` and mutates no binding; query from inside `Changed` and during `RefreshAll`; Player replacement (`Stale` for exact, new occurrence for Slot); Activity exit preserving a Session-owned Player; ambiguity and owner-exact selection; owner release and scene release compensation; a plain C# holder after scope release; exact owner equality with the composition owner.
5. Unity confirmation of current binding discovery (`OnSceneAvailable` only) and of the anchor's bind/release ordering.
6. Public API documentation, the Actor Occurrence References guide and `FrameworkApiStatus` metadata describe the query role as Experimental, its point-in-time semantics, its cost and the physical-versus-authorization boundary.
7. Consumer validation (for example PEGA) is a separate step against the resolved candidate package.

## 7. Consequences

- Consumers with runtime-known targets gain a legitimate path that does not misuse observation.
- The binding keeps a single, clear responsibility.
- The Framework adds one Experimental access contract and its tests and documentation.
- Consumers must handle point-in-time semantics and transient `Unavailable` during handoff.
