# IF-TRACK Framework — Audio delta to apply

Target: `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md` on current `master`.

This file intentionally contains only the Audio-related delta so unrelated mutable tracker state is not overwritten by this documentation package. Apply the additions/replacements below to the current tracker.

```diff
diff --git a/Documentation~/Architecture/Tracking/IF-TRACK-Framework.md b/Documentation~/Architecture/Tracking/IF-TRACK-Framework.md
--- a/Documentation~/Architecture/Tracking/IF-TRACK-Framework.md
+++ b/Documentation~/Architecture/Tracking/IF-TRACK-Framework.md
@@
 Current closure records:
@@
 - [IF-ADR-013 — BGM Continuity Technical Certification](../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md)
+- [IF-ADR-013 — Startup Activity BGM Lifecycle Reconciliation](../Reconciliation/IF-ADR-013-Startup-Activity-BGM-Lifecycle-Reconciliation-2026-08-24.md)
 - [Stage B — Game Flow Sample Consumer Evidence](../Reconciliation/IF-STAGE-B-GAMEFLOW-SAMPLE-EVIDENCE-2026-08-21.md)
@@
-| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / EXPERIMENTAL / IMPLEMENTED — IF-ADR-013A + BGM-CONTINUITY-1 + BGM-ROUTE-POLICY-1 | CERTIFIED: Audio 30/30 = Core 7/7 + Framework BGM 14/14 + ADR-013A 5/5 + physical continuity 4/4; real Framework Route A->B continuity PASS | **FIRSTGAME/SAMPLE CONSUMER GATE PASS** — Game Flow Sample proves Play, no-request Preserve, owner-exit preservation and explicit Silence across transient Route/Activity scenes. API remains Experimental pending an explicit product-maturity promotion cut. |
+| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / EXPERIMENTAL / IMPLEMENTED — Route/Activity BGM authoring independent; Startup ordering closed by ActivityFlow entry completion; persistent Director wired explicitly through FrameworkRuntimeHost | **CURRENT CERTIFIED: Audio 44/44 = Core 7/7 + Framework BGM 28/28 + ADR-013A 5/5 + physical continuity 4/4.** Historical 30/30 remains dated BGM-CONTINUITY-1 evidence. | **CONSUMER PASS** — Game Flow proves contextual Play/Preserve/Silence; Minimal Game proves Route PlayOwn with content-less Startup Activity; Player Provisioning proves Activity-owned BGM with no Route BGM binding. API remains Experimental pending explicit maturity promotion. |
@@
-### Audio BGM continuity — IF-ADR-013 / BGM-CONTINUITY-1 — 2026-08-19
+### Audio BGM current lifecycle closure — IF-ADR-013 — 2026-08-24
@@
 Architecture:
 
 ```text
 Framework Persistent Content
 └─ AudioRuntimeHost + FrameworkBgmDirector
-        ↑ runtime injection
-        │
-Transient Route/Activity BGM bindings
+        ↑ consumer injection into transient Route/Activity BGM bindings
+
+Activity entry completion
+  ActivityFlowRuntime
+    -> explicit receiver wiring
+    -> FrameworkBgmDirector
 ```
+
+Current authoring rule:
+
+```text
+FrameworkRouteBgmBinding
+  Route intent only
+
+FrameworkActivityBgmBinding
+  Activity intent only
+
+Route -> Activity BGM authoring reference
+  absent
+```
@@
-Automated certification:
+Current automated certification:
 
 ```text
 Core Audio         7/7 PASS
-Framework BGM     14/14 PASS
+Framework BGM     28/28 PASS
 ADR-013A            5/5 PASS
 Audio continuity    4/4 PASS
-TOTAL              30/30 PASS
+TOTAL              44/44 PASS
 FAILED               0
 ```
+
+Focused Startup Activity cases:
+
+```text
+startup-activity-neutral-baseline                   PASS
+startup-route-is-deferred                           PASS
+startup-activity-prevents-route-transient-play      PASS
+```
+
+Minimal Game proves `Route PlayOwn` with `ActivityContentProfile = null`, no Activity BGM binding and `activityContentHandles = 0`; entry completion applies the pending Route cue. Player Provisioning proves Activity-owned BGM with no Route BGM binding.
@@
-The current warning emitted when a Startup Activity has no explicit Startup BGM binding can occur in an intentionally BGM-neutral Route. That is diagnostic/product-surface debt, not a continuity defect; do not invent Play/Silence intent only to silence the warning.
+The former Route -> Startup Activity BGM authoring reference and its warning path are no longer part of the current contract. Startup ordering is closed by Activity lifecycle completion; do not add fake Activity BGM authoring to make Route BGM resolve.
@@
 Certification record:
 
 [IF-ADR-013 — BGM Continuity Technical Certification](../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md)
+
+Current lifecycle reconciliation:
+
+[IF-ADR-013 — Startup Activity BGM Lifecycle Reconciliation](../Reconciliation/IF-ADR-013-Startup-Activity-BGM-Lifecycle-Reconciliation-2026-08-24.md)
@@
-5. **Audio** — BGM-CONTINUITY-1 technical runtime/QA and Game Flow Sample real-consumer integration are closed. Remaining ADR-013 work, if desired, is an explicit API maturity promotion decision rather than another FIRSTGAME proof gate.
+5. **Audio** — current Route/Activity independent authoring, Startup Activity lifecycle completion and persistent Director wiring are technically closed at Audio QA 44/44. Game Flow, Minimal Game and Player Provisioning provide transversal consumer evidence. Remaining ADR-013 work, if desired, is explicit API maturity promotion rather than another runtime redesign/proof gate.
@@
 - [ADR-013 BGM Continuity Technical Certification](../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md)
+- [ADR-013 Startup Activity BGM Lifecycle Reconciliation](../Reconciliation/IF-ADR-013-Startup-Activity-BGM-Lifecycle-Reconciliation-2026-08-24.md)
 - [Stage B — Game Flow Sample Consumer Evidence](../Reconciliation/IF-STAGE-B-GAMEFLOW-SAMPLE-EVIDENCE-2026-08-21.md)

```
