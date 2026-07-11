# C9F — Camera output session

Status: implementation ready for Unity compile and QA validation  
Type: scoped runtime coordination  
Package: `com.immersive.framework`

## Objective

Remove manual synchronization between request mutations and output application.

```text
CameraOutputSession.Admit(request)
-> CameraOutputContext.Admit
-> CameraOutputRigApplicator.Apply
```

```text
CameraOutputSession.Release(requestId)
-> CameraOutputContext.Release
-> CameraOutputRigApplicator.Apply
```

## Architecture

`CameraOutputSession` composes:

```text
one CameraOutputContext
one CameraOutputRigApplicator
one CameraOutputId
```

It is not a global manager and does not discover outputs.

Winner selection remains exclusively inside `CameraOutputContext`.

## Transactional rule

An accepted context mutation must not leave arbitration and presentation divergent.

If output application fails:

```text
Admit
-> release the newly admitted request
-> reapply the previous winner
```

```text
Release
-> re-admit the released request
-> reapply the previous winner
```

Successful restoration returns:

```text
OperationKind = RolledBack
code = camera.output-session.application-failed-rolled-back
```

If rollback cannot fully restore consistency:

```text
OperationKind = RollbackFailed
code = camera.output-session.rollback-failed
```

No failure is hidden.

## Public surface

```csharp
var session = new CameraOutputSession(context, applicator);

CameraOutputSessionResult admit =
    session.Admit(request);

CameraOutputSessionResult release =
    session.Release(request.RequestId);

CameraOutputSessionResult synchronize =
    session.Synchronize();
```

## Out of scope

```text
Route/Activity/Player publishers
automatic lifetime observation
multi-output registry
global service
runtime Recipe materialization
blend policy
target rebinding
```

## Expected QA

```text
admit automatically applies first winner
higher request automatically replaces winner
release automatically restores previous winner
final release automatically clears output
blocked context mutation does not apply
application failure rolls back admission
application failure rolls back release
synchronize applies pre-existing context state
constructor rejects mismatched output
Unity Camera remains unchanged
```

## Suggested commit

```text
Camera: coordinate output mutations and application transactionally
```
