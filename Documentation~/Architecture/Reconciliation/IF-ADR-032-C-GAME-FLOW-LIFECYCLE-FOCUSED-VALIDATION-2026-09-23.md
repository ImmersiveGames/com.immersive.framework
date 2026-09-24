# IF-ADR-032-C Game Flow Lifecycle Focused Validation — 2026-09-23

Status: **FOCUSED QA VALIDATED — 16/16 PASS / canonical restore PASS**

Normative authority:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)

QA repository:

- `rinnocenti/QAFramework`
- menu: `Immersive Framework > QA > Camera > Run CAMERA-032 Game Flow Lifecycle Certification`

## Scope

This record captures Unity Play Mode technical evidence for CAMERA-032-C Route / Activity lifecycle integration and the transition force-default continuity required by IF-ADR-032.

The focused certification deliberately uses the current architecture:

~~~text
GameApplication Camera Session
  -> one explicit Session Output prefab
  -> optional Session Camera Presentation

Route A
  -> Route Camera Presentation
  -> startup Activity A

Activity A
  -> Activity Camera Presentation

Route B
  -> no Route Camera Presentation
  -> no startup Activity
~~~

The two generated Route primary scenes are temporary and contain no scene-owned Camera authority. This prevents a gameplay scene Camera from satisfying continuity during Route replacement.

## Certification shape

Two fresh boots execute the same ordered eight-case lifecycle.

### Session fallback boot

~~~text
runtime-ready
activity-winner-established
activity-exit-restores-route
route-force-default-preserves-normal-requests
route-force-default-renders-default
route-replacement-releases-outgoing-scene
route-exit-restores-lower-scope-or-default
route-force-default-release-restores-current-winner
~~~

The fallback below Route is a Session Camera Presentation.

### Default fallback boot

The second boot removes the Session Camera Presentation while retaining the physical Session Output and its Default Rig.

The same eight ordered cases run again. After Route B replaces Route A, normal Camera arbitration has no surviving request, so force-default release must restore the physical Output Default.

## Unity execution evidence

Observed on 2026-09-23:

~~~text
Session fallback
  cases = 8/8
  status = Passed

Default fallback
  cases = 8/8
  status = Passed

Focused total = 16/16
Canonical Shared baseline restore = PASS

Terminal verdict =
  CAMERA_032_GAMEFLOW_LIFECYCLE_CERTIFIED
~~~

The canonical Shared Camera QA baseline was rebuilt, persisted, verified and restored after the focused run.

## Contracts proven

### Activity lifecycle restoration

The startup Activity Presentation was the initial normal winner.

After `Clear Activity`:

~~~text
Activity request        Succeeded
Activity occurrence     Released
normal request count    dropped by Activity occurrence only
winner                  Route Presentation
force-default           inactive
physical application    normal request
~~~

Disposition:

~~~text
Activity -> Route restoration     PASS
~~~

### Force-default continuity

Force-default evidence is captured during Route replacement, not during zero-content Activity clear.

Observed:

~~~text
normal requests remain logically admitted
force-default becomes active
Output physically applies Default
Route lifecycle replacement continues
force-default releases after replacement
~~~

Disposition:

~~~text
force-default -> Default without destroying normal requests     PASS
~~~

### Route replacement

Route A primary scene was replaced by Route B using the normal Game Flow Route request path.

Observed:

~~~text
Route request                 Succeeded
Route A primary scene         retired
Route B primary scene         loaded and active
transition surface            SucceededWithUnitySurface
loading surface               SucceededWithUnitySurface
blocking issues               0
~~~

Because both generated Route primary scenes contain no scene-owned Camera authority, the successful replacement proves that continuity does not require the outgoing gameplay scene Camera to survive.

Disposition:

~~~text
Route replacement without outgoing gameplay Camera survival    PASS
~~~

### Session fallback

In the first boot, after Route A releases and Route B has no Route Presentation:

~~~text
surviving normal winner = Session Presentation
force-default release   = Session Presentation
~~~

Disposition:

~~~text
Route -> Session restoration     PASS
~~~

### Default fallback

In the second boot there is no Session Presentation.

After Route A releases:

~~~text
normal request count     0
normal winner            none
physical application     Output Default
force-default released   yes
~~~

Disposition:

~~~text
Route -> Default restoration     PASS
~~~

## Persistent Content boundary

The focused fixture uses Persistent Content only for genuinely persistent surfaces and the public Game Flow QA trigger host.

Before Play Mode the fixture verifies that Persistent Content contains no transitional Camera topology authority from:

~~~text
CameraOutputAuthoring
SessionCameraOverride
PlayerCameraOutputPolicyAuthoring
CameraSharedComposition
~~~

Physical Output materialization comes from the GameApplication Camera Session.

Disposition:

~~~text
Persistent Content is not required to contain gameplay Camera composition    PASS
~~~

## Non-blocking Unity warning

The generated Route primary scenes intentionally contain no Unity Camera and no AudioListener.

Unity emitted:

~~~text
There are no audio listeners in the scene.
Please ensure there is always one audio listener in the scene
~~~

during the short interval before Persistent Content / Session Camera materialization. This warning did not produce a Camera QA failure and was present in the terminal 16/16 certified run.

It is not evidence of Camera authority failure and must not be interpreted as a failed CAMERA-032-C contract.

## Acceptance criteria closed by this evidence

~~~text
Session / Output
  [x] force-default -> Default without destroying normal requests
  [x] release force-default -> current normal winner or Default

Game Flow
  [x] Activity exit restores Route/Session/Default correctly
  [x] Route exit restores Session/Default correctly
  [x] Route replacement never requires outgoing gameplay Camera survival

Authoring
  [x] Persistent Content is not required to contain gameplay Camera composition
~~~

## Explicitly not closed by this run

This focused certification does not prove:

~~~text
Startup HUB with no Session/Route Presentation -> Default
broader Basic Flow A/B/C consumer restoration matrix
fresh Camera Subject occurrence identity after rejoin/replacement
stale availability/selection/membership rejection
stale rollback protection
failed Rig/request admission/release rollback matrix
owner-safe cross-scope failure recovery
CAMERA-032-F legacy cleanup and final aggregate recertification
~~~

Those remain separate IF-ADR-032 closure work.

## Result

~~~text
CAMERA-032-C implementation                    PASS
Unity Play Mode                                PASS
Activity -> Route restoration                  PASS
force-default request preservation             PASS
force-default physical Default                 PASS
Route replacement                              PASS
Route -> Session restoration                   PASS
Route -> Default restoration                   PASS
force-default release continuity               PASS
camera-free Route replacement proof            PASS
Persistent Content Camera-independence proof   PASS
Session fallback cases                         8/8 PASS
Default fallback cases                         8/8 PASS
Focused QA total                              16/16 PASS
Canonical Shared baseline restore              PASS

CAMERA-032-C focused technical validation       YES
Full IF-ADR-032 certification                   PENDING
~~~
