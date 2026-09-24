# IF-ADR-032 Two-Output Player Lifecycle Focused QA — 2026-09-23

Status: **FOCUSED QA PASS / CAMERA-032-D/E MULTIPLAYER SLICE VALIDATED**

Scope:

- CAMERA-032-D explicit two-Output Session topology;
- runtime-only Cinemachine Output channel isolation;
- zero-Player Default continuity;
- Player Slot -> exact physical Output projection;
- CAMERA-032-E Player Slot -> `ExplicitSelection` Presentation binding;
- independent P1/P2 ThirdPerson presentation;
- PlayerInputManager-owned split-screen recomposition;
- Leave/rejoin physical Output lifecycle;
- canonical QA baseline restoration.

This record does **not** certify the full IF-ADR-032 acceptance matrix.

## 1. Validation basis

QAFramework menu:

~~~text
Immersive Framework
  > QA
    > Camera
      > Run CAMERA-032 Player Output Lifecycle Certification
~~~

Captured run:

~~~text
topology='GameApplicationSessionOutputs'
cycle='0->1->2->1->2->0'
presentations='ExplicitSelectionThirdPerson'
~~~

Framework baseline for the run includes:

- exclusive Cinemachine channel assignment per materialized Output occurrence;
- Presentation occurrence channel inheritance from its exact Output;
- zero-Player Session continuity for Default/Route/Session presentation;
- dormant unassociated Player-bound Outputs while another Player Output is associated;
- no synthetic `false -> true -> false` PlayerInputManager split-screen recomposition.

## 2. Boot and Session Output evidence

The run materialized exactly two explicit Session Outputs:

~~~text
Camera Session Outputs materialized.
outputCount='2'
~~~

Boot then completed successfully with:

~~~text
gameApplication='QA CAMERA-032 Player Output Lifecycle'
primaryScene='QA_Player'
routeSceneComposition='Succeeded'
activityReadiness='Ready'
blockingIssues='0'
~~~

This proves the focused QA fixture is exercising the current GameApplication Camera Session path rather than the historical scene-owned Player Camera policy path.

## 3. Captured QA cases

The supplied run completed:

~~~text
runtime-ready
zero-player-default-continuity
exclusive-cinemachine-channels
p1-joined-exact-output
p1-third-person-full-screen
p2-joined-exact-output
two-player-split-coherent
two-player-third-person-isolated
p2-left-output-dormant
p1-restored-full-screen
p1-third-person-survives-leave
p2-rejoined-fresh-host
split-restored-after-rejoin
terminal-default-continuity
~~~

Terminal runtime result:

~~~text
status='Passed'
cases='14/14'
diagnostic='ADR-032 two-Output Player lifecycle and zero-Player Default continuity passed.'
~~~

Terminal certification result:

~~~text
status='Passed'
verdict='CAMERA_032_PLAYER_OUTPUT_CERTIFIED'
cases='14/14'
cycle='0->1->2->1->2->0'
thirdPerson='PASS'
channels='PASS'
leaveRecompose='PASS'
canonicalRestore='PASS'
~~~

## 4. Two-Output / Cinemachine isolation

The certification proves:

~~~text
Output P1
  -> exclusive Cinemachine channel
  -> P1 ThirdPerson Presentation occurrence

Output P2
  -> different exclusive Cinemachine channel
  -> P2 ThirdPerson Presentation occurrence
~~~

The two materialized ThirdPerson occurrences are distinct and each uses the channel of its exact Output.

This closes the multi-Brain routing gap that previously allowed both physical Brains to consider the same virtual Camera.

## 5. Zero / one / two Player physical lifecycle

The validated physical lifecycle is:

~~~text
0 Players
  -> Session Output continuity remains available
  -> Default presentation is applied
  -> split-screen off

P1 joins
  -> P1 exact Output active
  -> P2 Player-bound Output dormant
  -> P1 ThirdPerson full-screen

P2 joins
  -> both exact Outputs active
  -> split-screen coherent
  -> P1/P2 ThirdPerson selections isolated

P2 leaves
  -> P2 Output dormant
  -> P1 returns full-screen
  -> P1 ThirdPerson remains authoritative

P2 rejoins
  -> fresh PlayerInput host
  -> P2 exact Output association restored
  -> split-screen restored

final Leaves
  -> zero-Player Default continuity restored
~~~

No Player count -> Output synthesis occurs. The two Outputs are explicit Session capacity throughout the run.

## 6. Leave recomposition result

The captured run completed the Leave/rejoin path without the Unity Input System error:

~~~text
Player has no camera associated with it.
Cannot set up split-screen.
~~~

There is no occurrence of that message in the supplied certification log.

The validated implementation does not force split-screen back to `true` during a Leave merely to reset the surviving viewport. The real `true -> false` transition remains owned by PlayerInputManager and restores the surviving Camera to full-screen.

Camera continues not to write `Camera.rect` directly.

## 7. Non-blocking QA fixture noise

The run contains repeated Unity messages reporting no AudioListener in the focused QA scene.

Those messages are unrelated to Camera authority, Player Output binding, Cinemachine routing or split-screen recomposition and do not affect the Camera certification verdict.

No Framework `[ERROR]`, Framework `[WARNING]`, `status='Failed'` or exception evidence appears in the supplied run.

## 8. Evidence contribution

This focused QA closes the following IF-ADR-032 items:

~~~text
CAMERA-032-D
  two explicit Session Outputs materialize                    PASS
  exclusive Cinemachine Output channels                      PASS
  zero-Player Default continuity                             PASS
  Player-bound Output physical participation                 PASS
  2 -> 1 split-screen recomposition                          PASS
  terminal return to Session continuity                     PASS

CAMERA-032-E
  Player Slot -> Output exact mapping                        PASS
  Player Slot -> ExplicitSelection Presentation             PASS
  P1/P2 independent ThirdPerson occurrences                 PASS
  P1/P2 selected Subject isolation                          PASS
  P2 rejoin creates a fresh PlayerInput host                PASS

QA hygiene
  canonical Shared Camera QA baseline restored              PASS
~~~

Still pending:

~~~text
explicit Camera Subject identity freshness assertion after rejoin/replacement
Activity -> Route restoration
Route -> Session/Default restoration
Route replacement continuity
force-default apply/release continuity
stale Subject / stale rollback focused QA
transaction-failure integrity matrix
CAMERA-032-F legacy cleanup and full recertification
~~~

## 9. Consumer documentation disposition

The FIRSTGAME split-screen sample was useful for discovering the multi-Output channel and Leave lifecycle gaps, but it remains under active sample evolution.

Therefore this record does not freeze or certify that consumer composition and does not require FIRSTGAME documentation updates.

The authoritative technical proof for this slice is the QAFramework certification above.

## 10. Disposition

~~~text
CAMERA-032-D two-Output Player slice          VALIDATED
CAMERA-032-E ExplicitSelection Player slice   VALIDATED
Focused QA                                   14/14 PASS
Full IF-ADR-032 certification                 PENDING
~~~

Related evidence:

- [IF-ADR-032-A Camera Presentation Runtime Focused Validation — 2026-09-21](IF-ADR-032-A-CAMERA-PRESENTATION-RUNTIME-FOCUSED-VALIDATION-2026-09-21.md)
- [IF-ADR-032 Local Multiplayer Group Consumer Proof — 2026-09-22](IF-ADR-032-LOCAL-MULTIPLAYER-GROUP-CONSUMER-PROOF-2026-09-22.md)

Normative authority remains:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)
