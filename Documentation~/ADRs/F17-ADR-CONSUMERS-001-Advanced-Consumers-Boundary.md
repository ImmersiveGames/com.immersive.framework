# F17-ADR-CONSUMERS-001 - Advanced Consumers Boundary

Status: Deferred / Future adapter module detail  
Original phase: F17 - Advanced Consumers  
Current placement: F28/F29+ / Adapter Modules  
Type: Consumers / Boundary / Integration  
Last updated: 2026-06-26

---

## 1. Context

This ADR preserves the boundary for future advanced consumers such as Camera, Audio, Actor and Pooling.

The original phase number is historical. Advanced Consumers must not be treated as framework core or as the current next cut. They belong under the F28/F29 adapter module spine.

---

## 2. Current Decision

Advanced Consumers are deferred to F28/F29+ planning and adapter module implementation.

They can be reconsidered only after:

```text
F23 - Pause Content / Overlay / Input Intent Boundary
F24 - Unity Build Surface / Lifecycle Wiring
F25/F26 - Activity scene operation and loading progress stabilization
F27 - Pause UIGlobal / input evidence
F28 - Roadmap Reconciliation and Adapter Module Spine
```

They also depend on a mature enough model for gameplay objects/actors where relevant.

---

## 3. Deferred Consumers

Future advanced consumers may include:

```text
Camera
Audio
Actor
Pooling integration
pooled materializer
```

They must consume existing core contracts instead of defining them:

```text
Gate
Lifecycle
Content Anchor
Runtime Materialization
RuntimeContentHandle
Object Entry
Reset
Contribution
Release
Diagnostics
```

---

## 4. Still Excluded

This deferred ADR still excludes gameplay-heavy systems:

```text
Projectile gameplay
Damage system
Attributes system
Powerups
Combat rules
Inventory
Enemy AI framework
contextual Player/Actor/NPC/Timer/Door/Pickup reset
```

---

## 5. Guardrails

- No advanced consumer can create a parallel lifecycle.
- No advanced consumer can discover the world globally as source of truth.
- No advanced consumer can use name/path as functional identity.
- No advanced consumer can create a silent fallback when a required dependency is missing.
- No advanced consumer can transform reset into release or pool return.
- No advanced consumer should bypass Gate when flow admission is required.

---

## 6. What Not To Change Now

Do not implement Camera, Audio, Actor, gameplay Pooling, Projectile, Damage, Attributes or Powerups in F17-F21.
