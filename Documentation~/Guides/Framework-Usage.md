# Framework Usage

Status: Current  
Last updated: 2026-08-06

## 1. Product workflow

1. Create a `GameApplicationAsset`.
2. Configure ordered Player Slots and explicit application policies.
3. Create `RouteAsset` and `ActivityAsset` assets.
4. Configure Route and Activity content, participation, transition and Gate policies.
5. Create and assign one Persistent Content Scene.
6. Author gameplay features through their official Composer/Authoring surfaces.
7. Use explicit Apply/Rebuild only where derived materialization exists.
8. Validate through the owning Inspector.
9. Enter Play Mode and inspect runtime evidence separately from authoring evidence.

Missing required contracts block explicitly. The framework does not repair configuration through hidden lookup.

## 2. Authority model

```text
GameApplicationAsset
→ bootstrap
→ Persistent Content load and retention
→ internal FrameworkRuntimeHost
→ Session
→ Route lifecycle
→ Activity lifecycle
→ scoped feature contexts and modules
```

`FrameworkRuntimeHost` is an internal composition root. It is not a public service locator and should not be found through static/global lookup.

## 3. Persistent Content

The Game Application declares one Content Scene.

Example:

```text
PersistentContent.unity
  physical Camera Output
  Presentation Canvas
  Transition surface
  Loading surface
  Pause presentation
  optional Player provisioning
  optional Audio composition
```

The scene is the concrete visual composition authority. Prefabs may be used inside it, but the Game Application does not separately declare those prefabs.

### 3.1 Create the scene

Preferred flow:

```text
File
  New Scene
    Immersive Persistent Content
```

Assign the resulting `.unity` scene to the Game Application and enable it in the active Build Profile.

### 3.2 Minimal contract

Exactly one physical Camera Output is required:

```text
CameraOutputSessionBinding
```

It requires an explicit Output ID and explicit Camera/Brain references.

`SessionCameraOverrideBinding` is optional. Player, Route and Activity Camera requests use the physical output without creating an implicit Session request.

Transition, Loading and Pause presentation are optional. Missing optional adapters resolve to explicit NoOp behavior; no fallback object is created.

### 3.3 Validation

Run:

```text
Validate Configuration
```

Validation is explicit and non-mutating. Inspector repaint does not open scenes, create objects or repair configuration.

## 4. Authoring and materialization

Use the product layers intentionally:

```text
Recipe / Profile / Template
  reusable intent

Composer / Authoring Component
  concrete scene or prefab configuration

Materialization
  explicit technical components and bindings

Runtime Context / Session / Service
  scoped runtime authority

Diagnostics
  validators, snapshots, reports and smokes
```

Apply/Rebuild must be:

- explicit;
- idempotent;
- Undo-aware;
- non-destructive;
- diagnostic;
- limited to derived technical materialization.

Authoring components do not execute gameplay by accident.

## 5. Runtime diagnostics

The normal Inspector remains designer-first. Technical evidence belongs in:

```text
Advanced / Debug
```

Persistent runtime diagnostics may project immutable values from the internal host. They must not:

- retain scene object references;
- create a second authority;
- mutate runtime state;
- survive across Play Sessions;
- require polling or scene search.

### 5.1 Scene-Provided Player release diagnostics

After a Scene-Provided Player scene unloads, inspect:

```text
FrameworkRuntimeHost
  Advanced / Debug
    Scene-Provided Admissions
```

The projection shows:

- active admission count;
- occupied Slot count;
- last operation/status;
- typed Slot and authored Actor identity;
- source/reason;
- release success or idempotence;
- post-operation Host-evidence presence.

This is direct diagnostic evidence that a release completed. It is not an admission/release command surface.

See `Player-Usage.md`.

## 6. Logging

Use `FrameworkLogger` only.

Recommended development profile:

```text
Default Minimum Level = Info
```

Operational milestones remain visible at Info. Detailed technical snapshots belong at Debug/Trace and in Inspector diagnostics.

Do not use the Console as the primary authoring surface.

## 7. Manual validation order

For a package technical cut:

```text
1. package compiles
2. QA consumer imports and compiles
3. focused QA smoke/negative proof
4. FIRSTGAME real integration
5. documentation freeze
```

For a UX/product cut:

```text
1. define the user-facing surface
2. prove assembly and comprehension in FIRSTGAME
3. confirm technical contracts
4. formalize in the package
5. add QA after the contract stabilizes
```

A smoke pass alone does not close product usability.

## 8. Current Player checkpoint

The Scene-Provided Player comparison baseline is approved in FIRSTGAME for:

- admission;
- Slot `player.1`;
- Host join;
- Logical Actor adoption;
- Activity readiness;
- movement;
- gameplay Camera;
- Player-bound Pause;
- Object/Group Reset;
- Activity Restart;
- Route release;
- same-session reentry;
- persistent release diagnostics;
- teardown without the previous identity exception.

The next consumer comparison is Manager-Provisioned Player assembly. Session-Persistent Player remains blocked by an official package gap.

## 9. Do not introduce

- implicit managers;
- global service locators;
- static runtime host access;
- name/tag scene lookup;
- fallback Slot or Actor selection;
- hidden materialization;
- runtime reflection without an explicit decision;
- consumer-owned substitutes for official package authority.
