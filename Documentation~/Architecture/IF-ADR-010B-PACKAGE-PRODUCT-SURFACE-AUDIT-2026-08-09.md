# IF-ADR-010B — Package Product Surface Audit

Date: 2026-08-09  
Type: UX/Product audit / documentation  
Status: **CLOSED — current package surface rebaselined**  
Implementation changes: **none**  
FIRSTGAME changes: **none**  
QAFramework changes: **none**

## 1. Baseline

```text
com.immersive.framework
  eb39c574e9ca04db0f88c4eb8e0eb704a1902194
  P0 — Serialized Player Migration Integrity

QAFramework
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
  P0 — Serialized Player Migration Integrity
```

Normative basis:

```text
IF-ADR-010 — Editor and Inspector Product Surface Authority
Status: Accepted
Rule: manual explicit authoring is the default
Rule: ADR-010 compliance is not measured by tooling/automation quantity
```

This audit evaluates the package against the accepted ADR-010 standard. It does
not treat historical gap lists as current truth when the current Git state shows
a different product surface.

---

## 2. Executive verdict

The package does **not** have a generalized lack of Editor tooling.

The dominant current state is:

```text
broad product-surface implementation
+
inconsistent historical documentation
+
uneven Editor QA evidence
+
a few presentation-normalization opportunities
```

The audit found no evidence that the framework currently needs a new generic:

```text
Wizard system
Composer framework
Auto Setup system
Fix Everything workflow
Create Everything workflow
global authoring manager
second validation architecture
```

The accepted ADR-010 rule is therefore validated by the package:

```text
manual explicit authoring remains a first-class canonical path
```

The strongest current Class C / materialized-composition reference is Camera Rig
Authoring.

Player, Pause, Activity Restart, Reset and Readiness already demonstrate strong
Class A/B direct authoring surfaces.

Persistent Content is no longer classified as "missing". Its current lifecycle
was rebaselined as an official Scene Template whose source scene owns the
composition and whose pipeline performs non-mutating verification.

The previously suspected Unity Input Gate "missing product surface" is also not
confirmed. Its current Inspector is semantically substantial. Its differences
from the shared Inspector helper are normalization opportunities, not sufficient
evidence of ADR-010 non-compliance.

The largest cross-cutting gap exposed by this audit is **current Editor QA
evidence**, not missing automation.

---

## 3. Audit method

Each consumer-facing surface was evaluated against the applicable ADR-010
questions:

```text
Official Path
Intent First
Configuration Status
Actionable Diagnostics
Safe Explicit Remediation
Advanced / Debug
Editor Write Safety
Runtime Discipline
Risk-Appropriate QA
```

Conditional capabilities were evaluated only where they already exist:

```text
Profile / Recipe / Template
Composer
Apply / Rebuild
Create action
Wizard
materialization receipt
```

No feature was penalized merely for not having one of those conditional
capabilities.

### Product-surface classification

```text
COMPLIANT
PARTIAL
NON-COMPLIANT
REBASELINE REQUIRED
NOT APPLICABLE
```

QA evidence is recorded separately so a good product surface is not
misrepresented as missing merely because its Editor regression suite has not yet
been indexed or certified.

---

## 4. Shared Editor foundations

### 4.1 FrameworkAuthoringInspectorGui

Evidence:

```text
Editor/Common/FrameworkAuthoringInspectorGui.cs
```

Current shared vocabulary includes:

```text
ProductHeader
IntentSummary
Section
RuntimeBinding
AdvancedFoldout
ApplySuggestion
```

`ApplySuggestion` records Undo, applies serialized properties, marks targets dirty
and records prefab-instance modifications.

Classification:

```text
Product surface: NOT APPLICABLE
Role: FOUNDATION
State: STRONG
```

Interpretation:

The framework already has a reusable Inspector grammar and a safe deterministic
suggestion helper.

ADR-010 does not require every Custom Editor to call this helper literally.
Consistency is semantic, not identical UI implementation.

A Custom Editor should reuse the helper when it improves consistency without
forcing a lifecycle into an artificial presentation shape.

---

### 4.2 Authoring validation infrastructure

Evidence:

```text
Editor/Validation/FrameworkAuthoringValidationIssue.cs
Editor/Validation/FrameworkAuthoringValidationReport.cs
Editor/Validation/FrameworkAuthoringValidationGui.cs
Editor/Validation/FrameworkAuthoringModelReadinessAggregator.cs
```

Current typed issue contract actually contains:

```text
Severity
Message
Context
IsOptionalSkip
```

The GUI additionally provides:

```text
summary counts
issue HelpBoxes
Select/Ping context
structured report logging
```

Classification:

```text
Product surface: NOT APPLICABLE
Role: FOUNDATION
State: SUBSTANTIAL
```

Important correction to earlier audit language:

The current Git contract does **not** prove structured fields for:

```text
Category
Hint
Asset
Context Label
```

as independent members of `FrameworkAuthoringValidationIssue`.

Corrective guidance can still be expressed in `Message`, and `Context` provides
selection/ping support.

No new validation architecture is justified by this audit.

A future typed remediation/hint field should be introduced only if repeated
concrete product cases prove that the current `Message + Context` contract is
insufficient.

---

## 5. Product-surface matrix

| Area / Surface | Lifecycle | Package surface | QA evidence | 010B disposition |
|---|---|---|---|---|
| Shared Inspector GUI | Support | N/A — foundation | N/A | STRONG FOUNDATION |
| Shared Authoring Validation | Support | N/A — foundation | partial/current use | SUBSTANTIAL FOUNDATION |
| Player Participation / Provisioning | A + B | COMPLIANT | strong Player QA | KEEP |
| Activity asset/profile authoring | B | COMPLIANT | technical QA exists; Editor QA uneven | KEEP |
| Activity Request Trigger | A | COMPLIANT | not separately certified as Editor surface | KEEP; optional presentation normalization |
| Route asset/profile authoring | B | COMPLIANT | technical QA exists; Editor QA uneven | KEEP |
| Route Request Trigger | A | COMPLIANT | not separately certified as Editor surface | KEEP; optional presentation normalization |
| Game Application / Project Settings | B / Settings | COMPLIANT | product QA not yet canonical | KEEP |
| Camera Rig Composer | C | COMPLIANT / REFERENCE | dedicated Editor contract not found in current QA search | PRIORITY FOR 010C |
| Pause Request | A | COMPLIANT | broader technical evidence exists | KEEP |
| Unity Input Gate | A | COMPLIANT SEMANTICALLY | dedicated current QA not found | 010C candidate; normalization optional |
| Activity Restart | A | COMPLIANT | broader technical evidence exists | KEEP |
| Object Reset Group Trigger | A | COMPLIANT | broader Reset evidence incomplete | KEEP / 010C candidate |
| Activity Readiness Participant | A | COMPLIANT | readiness runtime QA strong; Editor QA not canonical | KEEP |
| Persistent Content Scene Template | B / Template | COMPLIANT PACKAGE SURFACE | consumer proof deferred | REBASELINE RESOLVED |
| Loading / Transition standalone authoring | unresolved | REBASELINE REQUIRED | runtime evidence strong | map actual consumer lifecycle before tooling |
| Diagnostics-only technical surfaces | Support | NOT APPLICABLE | diagnostic evidence | KEEP AS SUPPORT |
| Camera technical bindings | Support | NOT APPLICABLE | technical evidence | KEEP AS SUPPORT |

The matrix deliberately distinguishes a product-surface gap from a QA-evidence
gap.

---

## 6. Player Participation / Provisioning

Representative evidence:

```text
Editor/PlayerParticipation/PlayerProvisioningCommandTriggerEditor.cs
```

Observed product grammar:

```text
Product Header
Intent Summary
Command
Scoped Consumer Access
operation-specific authored parameters
Request Metadata
Actions
Configuration Status
Play Mode Runtime Binding
Last Typed Result
Advanced / Debug
```

Runtime execution is explicit and disabled outside Play Mode.

The Inspector explicitly states that there is no automatic:

```text
Awake
OnEnable
Start
OnValidate
```

command path.

Classification:

```text
Lifecycle: A for command/status components
           B where reusable Player Profiles are used

Product surface: COMPLIANT
```

Conclusion:

Historical statements saying Player needs a Composer/Wizard merely to become a
valid ADR-010 product surface are stale.

No Player Composer, Wizard or auto-setup cut is justified by ADR-010B.

---

## 7. Pause

Representative evidence:

```text
Editor/Pause/PauseRequestTriggerEditor.cs
```

Observed:

```text
Product Header
Intent Summary
Request Metadata
safe suggested diagnostic reason
Configuration Status
Play Mode Runtime Binding
Effective Pause Evidence
explicit Pause / Resume / Toggle actions
Advanced / Debug
```

The suggested reason is a deterministic diagnostic convenience, not gameplay
intent.

Classification:

```text
Lifecycle: A — Simple / Direct Authoring
Product surface: COMPLIANT
```

Conclusion:

A Pause Composer or Wizard would be over-authoring under the current evidence.

---

## 8. Activity Restart

Representative evidence:

```text
Editor/ActivityRestart/ActivityRestartTriggerEditor.cs
```

Observed:

```text
Product Header
Intent Summary
Activity Target
Reset Selection
Request Metadata
specific Configuration Status
Play Mode Runtime Binding
Runtime Request Evidence
explicit runtime request
Advanced / Debug raw results
```

Classification:

```text
Lifecycle: A
Product surface: COMPLIANT
```

No additional authoring layer is justified.

---

## 9. Reset

Representative evidence:

```text
Editor/Reset/ObjectResetGroupTriggerEditor.cs
```

Observed:

```text
Product Header
Intent Summary
stable Group ID
explicit Generate ID suggestion
Reset Selection
Request Metadata
specific Configuration Status
Play Mode Runtime Binding
Runtime Request Evidence
explicit Group Reset request
Advanced / Debug
```

The ID suggestion uses the shared safe suggestion path.

Classification of the inspected primary surface:

```text
Lifecycle: A
Product surface: COMPLIANT
```

The broader Reset editor family is substantial. ADR-010C should choose only
applicable risks for regression coverage rather than create a generic Reset
Composer.

---

## 10. Activity and Route

Current package contains large authoring families for:

```text
ActivityAsset
ActivityContentProfile
RouteAsset
RouteContentProfile
RouteContentBinding
Activity/Route reset triggers
Activity/Route request triggers
validators and scene-reference utilities
```

Representative request surfaces:

```text
Editor/GameFlow/ActivityRequestTriggerEditor.cs
Editor/GameFlow/RouteRequestTriggerEditor.cs
```

Both provide:

```text
authored target
request reason
safe suggested reason
Validate
specific validation result
Advanced / Debug
Play Mode Runtime Binding
Runtime Request Evidence
explicit supported runtime commands
```

They do not currently use the exact same `ProductHeader + IntentSummary` header
shape as Player/Pause/Restart.

Under accepted ADR-010 this is **not by itself non-compliance** because:

```text
consistency is semantic, not identical visual structure
```

Their normal fields are authored product intent, not internal technical details.

Classification:

```text
Activity authoring: COMPLIANT
Route authoring: COMPLIANT
```

Disposition:

Optional presentation normalization may be done during future maintenance, but it
is not a product blocker and should not outrank missing QA evidence or an actual
consumer problem.

---

## 11. Activity Readiness / Loading contribution

Representative evidence:

```text
Editor/Authoring/ActivityReadinessParticipantEditor.cs
```

The Inspector exposes:

```text
Participant Id
Requiredness
Order
Preparation callbacks
explicit configuration errors
Advanced / Debug read-only:
  State
  Occurrence
  Last Reason
```

Tooltips explicitly state that object names and hierarchy paths are not fallback
identity.

Classification:

```text
Lifecycle: A
Product surface: COMPLIANT
```

This is a direct authoring component. No Recipe, Composer or Apply/Rebuild is
needed to satisfy ADR-010.

The broader standalone Loading/Transition authoring lifecycle was not fully
resolved by this audit and remains a targeted rebaseline item before any new
product tooling is proposed.

---

## 12. Game Application / Project Settings

Evidence:

```text
Editor/Settings/ImmersiveFrameworkSettingsProvider.cs
```

Official path:

```text
Project > Immersive Framework
```

Observed:

```text
Editor Play Mode configuration
Active Game Application
project status
Logging configuration
Create/Open/Replace asset actions
Validate Configuration
Advanced / Diagnostics
Model Readiness
configuration file locations
explicit ownership explanation
```

The create actions are legitimate convenience actions because they create
deterministic framework configuration assets. They do not invent gameplay state.

Classification:

```text
Lifecycle: B / Project Settings
Product surface: COMPLIANT
```

No additional Wizard is justified.

---

## 13. Camera Rig Authoring

Evidence:

```text
Editor/CameraAuthoring/CameraRigComposerEditor.cs
Editor/CameraAuthoring/CameraRigComposerApplyRebuildResult.cs
```

Camera explicitly separates:

```text
Camera Behavior
        ↓
Materialization
        ↓
Apply / Rebuild Rig
        ↓
Validation
        ↓
Advanced / Diagnostics
```

The Inspector states that the Composer is authority only for one concrete local
Camera rig and explicitly does not create:

```text
Unity Camera
Cinemachine Brain
Audio Listener
persistent Camera Output
```

Materialization result evidence includes:

```text
Succeeded
Status
BlockingIssue
TargetResolutionSummary
MaterializationSummary
CreatedCount
RepairedCount
AlreadyValidCount
SkippedCount
BlockedCount
```

Classification:

```text
Lifecycle: C — Materialized Composition
Product surface: COMPLIANT
Role: CURRENT CLASS C REFERENCE
```

This is the correct reference for systems that truly require technical
materialization.

It is **not** a template that every framework feature must imitate.

### Open evidence gap

The current QA repository search did not locate a dedicated regression identified
by `CameraRigComposer` / `Apply Rebuild`.

Therefore the product surface can be retained as the reference, while ADR-010C
should explicitly prove its high-risk Editor contracts:

```text
idempotent Apply/Rebuild
Undo/Redo
Prefab Stage
framework-owned content preservation
user-owned content preservation
repeated Apply convergence
diagnostic result
```

This is a QA gap, not a reason to add more Camera tooling.

---

## 14. Unity Input Gate

Evidence:

```text
Editor/UnityInput/UnityPlayerInputGateAdapterEditor.cs
```

Observed current capabilities include:

```text
target PlayerInput / action-map configuration
Gate Policy
authoring status
Runtime Binding status
Advanced / Debug
physical application evidence
runtime diagnostics
explicit technical runtime commands
```

The Inspector does not use the exact shared `FrameworkAuthoringInspectorGui`
presentation grammar used by Player/Pause/Restart.

The preliminary audit treated that difference as `PARTIAL`.

After applying the accepted ADR-010, that conclusion is too strong.

ADR-010 explicitly says:

```text
consistency is semantic rather than identical visual structure
```

and compliance is not measured by tooling quantity or helper usage.

Current classification:

```text
Lifecycle: A
Product surface: COMPLIANT SEMANTICALLY
Presentation normalization: OPTIONAL
```

### Actual open gap

No dedicated current QA evidence was found in QAFramework for:

```text
UnityPlayerInputGateAdapter
Input Gate
```

Therefore the meaningful next question is not:

```text
"Should we build a new Input Gate product surface?"
```

It is:

```text
"Which existing Input Gate Editor risks need canonical regression coverage?"
```

Possible presentation normalization can be bundled only if a concrete UX problem
is demonstrated. It is not currently a blocker.

---

## 15. Persistent Content

Evidence:

```text
Editor/SceneTemplates/PersistentContent/
  ImmersivePersistentContent.scenetemplate
  PersistentContentTemplateSource.unity
  PersistentContentSceneTemplatePipeline.cs
```

The official template identifies itself as:

```text
Immersive Persistent Content
Application-persistent Camera, Transition and Loading composition
for the Immersive Framework.
```

The pipeline explicitly states:

```text
source scene owns the composition
pipeline performs non-mutating verification
pipeline never creates, repairs, saves or assigns consumer assets
```

After instantiation it validates the instantiated scene and logs the report.

This resolves the earlier `REBASELINE REQUIRED` finding.

Current lifecycle:

```text
Class B — reusable Template
```

Current classification:

```text
Product surface: COMPLIANT AT PACKAGE LEVEL
Consumer usability evidence: DEFERRED
```

Important consequence:

Persistent Content does **not** need a Composer/Apply flow merely because a
historical document once described one.

The current scene-template lifecycle is a legitimate ADR-010 product model.

FIRSTGAME can later prove whether the template is discoverable and understandable
in real usage, but that product proof is separate from the package-surface
classification.

---

## 16. Validation contract correction

The earlier preliminary audit overstated the current typed validation issue
shape.

Correct current contract:

```text
FrameworkAuthoringValidationIssue
  Severity
  Message
  Context
  IsOptionalSkip
```

Current GUI supports selecting/pinging Context and logging issue/report evidence.

Therefore:

```text
typed corrective Hint field      NOT CURRENTLY PRESENT
typed Category field             NOT CURRENTLY PRESENT
typed Asset field                NOT CURRENTLY PRESENT
typed Context Label field        NOT CURRENTLY PRESENT
```

This is not automatically a defect.

ADR-010 requires actionable diagnostics, not a particular data structure.

If existing messages repeatedly become ambiguous or remediation cannot be
represented safely, a future small contract cut may be justified.

Do not add those fields preemptively.

---

## 17. What this audit did NOT find

No current evidence justifies:

```text
generic Wizard framework
Player Composer requirement
Pause Composer
Reset Composer
Input Gate Composer
automatic complete-game setup
automatic gameplay remediation
generic Apply/Rebuild for direct components
new runtime authority
second authoring-validation system
```

No current feature should be marked incomplete simply because one of these layers
does not exist.

---

## 18. Current concrete gaps

### Gap A — Canonical Editor QA evidence

Priority: **HIGH**

The product package is ahead of the current canonical Editor QA story.

Highest-value lifecycle risks to certify:

```text
Class C / Camera:
  Apply/Rebuild idempotency
  non-destructive rebuild
  Undo/Redo
  Prefab Stage
  ownership preservation
  repeated Apply convergence

Class A direct triggers/adapters:
  Play Mode-only commands
  read-only runtime evidence
  multi-object behavior where supported
  explicit rejection where unsupported

Project Settings / asset creation:
  deterministic creation
  no duplicate/conflicting settings authority
```

This becomes the primary ADR-010C program.

---

### Gap B — Loading / Transition standalone product lifecycle

Priority: **MEDIUM**

Persistent Content proves one application-persistent composition path, but the
audit did not resolve whether Loading/Transition has another recurrent standalone
consumer authoring lifecycle that needs independent classification.

Required next action:

```text
map existing consumer-facing Loading/Transition contracts
identify whether they are:
  already owned by Activity/Route/Persistent composition
  simple direct authoring
  internal/support only
```

Do this before proposing tooling.

---

### Gap C — Presentation normalization

Priority: **LOW**

Examples:

```text
Unity Input Gate
Activity Request Trigger
Route Request Trigger
some older Custom Editors
```

These surfaces do not all use identical shared-header vocabulary.

Current evidence does not show that this prevents use or exposes internal
authority.

Treat as normal maintenance, not ADR-010 product failure.

---

## 19. First implementation cut disposition

The preliminary audit suggested:

```text
Unity Input Gate Product-Surface Normalization
```

as the first code cut.

The accepted ADR-010 changes that conclusion.

Because semantic compliance does not require identical helper usage, ADR-010B
does **not** establish enough evidence to justify an Input Gate code modification
before QA.

Current disposition:

```text
NO PRODUCT TOOLING IMPLEMENTATION REQUIRED FROM ADR-010B
```

The next justified cut is evidence-focused:

```text
IF-ADR-010C — Canonical Editor Product-Surface QA
```

This is deliberately not a new smoke-menu architecture.

It should define one canonical QA pattern and add only the smallest regressions
needed to prove the applicable Editor invariants.

---

## 20. Recommended ADR-010C order

```text
010C-1
Camera Rig Apply/Rebuild Editor Contract
  idempotency
  ownership preservation
  Undo
  prefab safety

010C-2
Direct Runtime Command Inspector Contract
  explicit Play Mode gating
  runtime evidence read-only
  multi-object behavior explicit

010C-3
Settings / deterministic asset creation
  only if current evidence shows a meaningful regression risk
```

Input Gate can be one representative Class A surface in 010C-2.

Do not build a QA matrix for every Inspector just to increase test count.

---

## 21. FIRSTGAME

FIRSTGAME remains deferred.

This audit does not claim current real-consumer usability evidence for the
redesigned product.

When FIRSTGAME is redesigned, it should consume these already accepted rules:

```text
manual explicit authoring is valid
surface must be understandable
invalid configuration must be explicit
technical evidence belongs in Advanced/Debug
automation must be justified by real friction
```

Observed real friction may later justify the smallest additional package
assistance.

---

## 22. ADR-010B acceptance

ADR-010B is closed when:

```text
current package surfaces are mapped
historical missing-tooling assumptions are rejected where stale
conditional tooling is not treated as mandatory
Camera is established as Class C reference
Persistent Content lifecycle is rebaselined
validation contract is described accurately
current concrete gaps are separated from cosmetic normalization
next QA cut is identified
```

Result:

```text
ADR-010 normative standard             ACCEPTED
ADR-010B package audit                 CLOSED
general missing tooling finding        NOT CONFIRMED
new automation required                NO
FIRSTGAME required for this closure    NO
next cut                               IF-ADR-010C
```

---

## 23. Suggested commit message

```text
Audit package product surfaces against ADR-010
```
