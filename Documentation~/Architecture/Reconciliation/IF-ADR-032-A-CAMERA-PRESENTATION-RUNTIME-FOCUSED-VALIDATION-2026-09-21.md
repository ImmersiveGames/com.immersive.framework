# IF-ADR-032-A Camera Presentation Runtime Focused Validation — 2026-09-21

Status: **FOCUSED QA VALIDATED**

Scope:

- IF-ADR-032 CAMERA-032-A — Presentation runtime extraction
- non-MonoBehaviour `CameraPresentationRuntime`
- transitional `CameraSharedComposition` Unity authoring/lifecycle adapter
- preserved Camera membership, presentation, request participation and rollback behavior

This record validates CUT A only. It does not certify the complete IF-ADR-032 migration.

## 1. Validation basis

Unity consumer state:

~~~text
compile/import                       PASS
~~~

QAFramework Camera Full was executed after CAMERA-032-A.

The Full orchestrator later failed in historical ADR-004B negative-integrity coverage, but the focused CUT A evidence had already completed successfully before that failure.

## 2. Structural evidence

The run started Camera runtime evidence with:

~~~text
structuralCases='10/10'
~~~

Disposition:

~~~text
CAMERA-032-A structural baseline     10/10 PASS
~~~

## 3. Shared Camera lifecycle evidence

The Shared phase completed all ten cases:

~~~text
shared-baseline-zero-subject-default
p1-join-fallback-framing
invalid-camera-authoring-rejected
fallback-root-subject
stale-replacement-rejected
explicit-replacement-framing-radius
p2-two-member-group
p1-leave-shrinks-group
p1-rejoin-fresh-occurrence
cleanup-zero-subject-default
~~~

Terminal result:

~~~text
phase='shared'
status='Passed'
cases='10/10'
playerIndependence='PASS'
fallbackObservation='PASS'
staleReplacement='PASS'
subjectFraming='ExplicitAndFallbackPASS'
idempotence='PASS'
compositionRequest='Explicit'
cleanup='TerminalClean'
~~~

This directly exercises the state moved into `CameraPresentationRuntime`:

- Subject availability and membership changes;
- stale occurrence rejection;
- presentation eligibility;
- request publication/release;
- cleanup back to Default.

Disposition:

~~~text
CAMERA-032-A Shared lifecycle        10/10 PASS
~~~

## 4. Generic request/output evidence

The canonical runtime-host Camera fixture completed:

~~~text
default-presentation
activity-request
route-request
session-request
session-release-restores-route
route-release-restores-activity
activity-release-restores-default
duplicate-request
duplicate-release
activity-lifecycle-cleanup
route-lifecycle-cleanup
~~~

Terminal result:

~~~text
phase='canonical-override-fixture'
status='Passed'
cases='11/11'
~~~

Disposition:

~~~text
CAMERA-032-A generic request/output  11/11 PASS
~~~

## 5. Focused CUT A result

~~~text
Structural                         10/10 PASS
Shared lifecycle                   10/10 PASS
Generic request/output             11/11 PASS
----------------------------------------------
CAMERA-032-A focused QA             31/31 PASS
Unity compile/import                PASS

CAMERA-032-A technically validated  YES
~~~

## 6. Full Camera aggregate failure is non-blocking for CUT A

After all focused CUT A evidence above had passed, the historical ADR-004B negative-integrity suite reported:

~~~text
case='17-duplicate-output-id'
status='Failed'
diagnostic='Canonical Persistent Camera composition regression did not execute the duplicate Output ID blocking case.'
~~~

ADR-004B therefore ended:

~~~text
cases='17/18'
failed='1'
~~~

and the Full Camera orchestrator reported the aggregate run as not certified.

This failure is outside the CAMERA-032-A extraction contract:

- it belongs to historical ADR-004B negative-integrity harness coverage;
- it is specifically a missing execution/evidence case for duplicate Output ID blocking;
- it occurs after Structural 10/10, Shared 10/10 and Generic 11/11 have already completed;
- CAMERA-032-A does not change duplicate Output ID validation, Output authoring or physical Output topology.

The Full orchestrator's terminal `sharedCases='0/10'` and `genericCases='0/11'` fields are aggregate accounting after the ADR-004B gate failed; they do not negate the explicit earlier terminal PASS records for those phases.

No QAFramework maintenance is required to close CAMERA-032-A.

If the Full Camera aggregate is reused for a later IF-ADR-032 certification, its historical ADR-004B rail should be removed, isolated or reconciled at that time rather than repaired as part of CUT A.

## 7. Disposition

~~~text
CAMERA-032-A implementation         PASS
Unity compile/import                PASS
Structural QA                       10/10 PASS
Shared Camera QA                    10/10 PASS
Generic request/output QA           11/11 PASS
Focused QA                          31/31 PASS
Technically validated               YES

Full legacy Camera aggregate        NOT CERTIFIED
Blocking for CAMERA-032-A           NO
QA legacy repair required now       NO

Next cut                            CAMERA-032-B
~~~
