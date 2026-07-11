# C9C — Camera Request Contracts

Status: Implemented contract package; Unity validation pending  
Package: `com.immersive.framework`  
ADR: `ADR-PROD-0006-camera-requests-output-contexts.md`

## Objective

Introduce the typed request vocabulary required before a runtime `CameraOutputContext` exists.

C9C defines contracts only:

```text
CameraOutputId
CameraRequestId
CameraRequestOwner
CameraRequestLifetime
CameraRigReference
CameraTargetSourceDescriptor
CameraRequestPolicy
CameraRequestReleaseCondition
CameraRequest
CameraRequestCreateResult
```

## Contract shape

A valid request identifies:

```text
one explicit output
one explicit request id
one typed owner
one typed lifetime
one recipe and/or materialized rig
one explicit target source
declarative precedence evidence
one release condition
diagnostic source and reason
```

Creation is fail-fast through `CameraRequestCreateResult`. Missing mandatory data blocks explicitly.

## Out of scope

C9C does not implement:

```text
CameraOutputContext
request registry
request admission
winner selection
tie resolution
release execution
Cinemachine priority or channel application
Route publisher
Activity publisher
Player publisher
fallback output creation
```

`CameraRequestPolicy` carries precedence and an optional deterministic tie-breaker id. It does not arbitrate. Ties remain invalid unless the later `CameraOutputContext` policy can resolve them deterministically.

## Expected manual validation

1. Import the package in QAFramework.
2. Confirm compilation.
3. Construct a valid request contract from a QA probe.
4. Confirm missing output blocks.
5. Confirm missing owner blocks.
6. Confirm missing lifetime blocks.
7. Confirm missing rig blocks.
8. Confirm missing target source blocks.
9. Confirm missing release condition blocks.
10. Confirm no output, camera or Cinemachine state is mutated.

## Next cut

```text
C9D — Single-output CameraOutputContext runtime
```
