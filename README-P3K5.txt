P3K.5 — Gameplay Admission and Camera Publication
==================================================

Precondition:
P3K.4 complete files must already be applied in this workspace.

Application:
1. Extract this ZIP at the root of com.immersive.framework.
2. Confirm replacement of the P3K.4 closure document.
3. Confirm creation of the P3K.5 contracts, runtime context and documentation.
4. Wait for Unity compilation before applying/running the QA ZIP.

Scope:
- consumes current P3K.2 occupancy;
- consumes current P3K.3 gameplay input binding;
- consumes current P3K.4 Eligible or SkippedOptional camera decision;
- publishes a LocalPlayer CameraRequest only for Eligible camera evidence;
- derives GameplayReady from current admission plus input availability;
- preserves camera publication during temporary Gate blocks;
- releases camera -> eligibility -> input -> occupancy;
- rolls back prerequisites when camera admission fails;
- retains retryable ReleaseFailed progress evidence.

Out of scope:
- Activity transition/admission gate integration;
- Actor materialization;
- camera winner selection outside CameraOutputContext;
- movement execution;
- FIRSTGAME integration.
