# Immersive Framework — IF-ADR-004 Camera Audit

> Historical record.
>
> This audit captures the framework state at the time it was written and is not
> current tracking or normative authority. For current Camera status, see the
> Tracker and the Architecture/Reconciliation records.

Date: **2026-08-10**  
Scope: **IF-ADR-004 + package Camera implementation + QA Camera evidence**  
Purpose: **Historical audit record with post-audit closure**

## Historical audit conclusion

The initial 2026-08-10 audit found a structurally sound Camera architecture and
recommended **no broad redesign** before focused negative QA. At that point:

```text
Architecture
  accepted / reconciliation required

Package
  substantially implemented

Positive QA
  present

Negative integrity QA
  incomplete

Abnormal owner lifetime
  unproven

Confirmed package defect
  none established by source inspection alone
```

That classification was intentionally evidence-conservative.

## Architecture found by the audit

```text
CameraRigComposer
  local designer intent/materialization

CameraOutputSessionBinding
  persistent physical Camera + CinemachineBrain

Scoped request publishers
  typed owner/lifetime publication

CameraOutputContext
  deterministic logical arbitration

CameraOutputRigApplicator
  physical projection

CameraOutputSession
  transactional synchronization + rollback
```

The audit also confirmed:

- equal precedence uses deterministic tie-break evidence, not newest-request
  timing;
- Persistent Content owns the single physical output;
- Route/Activity owner identity uses exact authored definitions;
- Follow is the current accepted presentation capability;
- no global Camera manager/service locator is justified.

## Historical unresolved question

The main hardening question was:

```text
admitted request
+ owner component disabled/destroyed before normal lifecycle exit
  -> does request survive beyond valid publication lifetime?
```

The audit correctly classified this as **unproven**, not as a source-only package
defect, and assigned the proof to IF-ADR-004B case 16.

## Post-audit resolution

The later executable evidence resolved the question:

### 1. 004B reproduced the defect

```text
case='16-abnormal-owner-loss'
admittedBefore='2'
admittedAfter='2'
orphan='True'
```

### 2. 004C fixed the narrow owner

The package correction was limited to scoped publication/component lifetime:

```text
ScopedCameraOverrideBinding.OnDisable
ScopedCameraOverrideBinding.OnDestroy
SessionCameraOverrideBinding owner-scope overrides
```

No global cleanup manager, registry or alternate Camera runtime was added.

### 3. Positive and negative QA both passed

```text
C9R      11/11 PASS
004C     10/10 PASS
004B     18/18 PASS
```

The 004B case 16 re-run reports `orphan='False'`.

## Current classification

```text
Normative Camera contract
  RECONCILED / ACCEPTED

Core runtime authority
  STRONG / IMPLEMENTED

Deterministic arbitration
  CERTIFIED

Transactional logical/physical consistency
  CERTIFIED

Persistent output authority
  CERTIFIED for accepted single-output boundary

Normal lifecycle cleanup
  CERTIFIED

Abnormal owner lifetime cleanup
  IMPLEMENTED + CERTIFIED by IF-ADR-004C

Negative QA
  CERTIFIED 18/18

FIRSTGAME
  broader real-product Camera proof remains separate/partial
```

## Historical value of this audit

The original finding that no broad redesign was justified remains valid. The one
real defect was discovered only after canonical QA reproduced it, and its fix
fit inside the existing scoped ownership architecture.

This audit should therefore be read as the evidence trail that led to 004A,
004B and 004C, not as current status saying negative QA is still pending.
