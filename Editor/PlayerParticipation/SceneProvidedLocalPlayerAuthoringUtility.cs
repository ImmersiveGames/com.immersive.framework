using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    public readonly struct SceneProvidedLocalPlayerAuthoringResult
    {
        public SceneProvidedLocalPlayerAuthoringResult(bool succeeded, SceneProvidedLocalPlayerAuthoringStatus status, string message, bool evidenceCreated, bool evidenceUpdated)
        {
            Succeeded = succeeded;
            Status = status;
            Message = message ?? string.Empty;
            EvidenceCreated = evidenceCreated;
            EvidenceUpdated = evidenceUpdated;
        }
        public bool Succeeded { get; }
        public SceneProvidedLocalPlayerAuthoringStatus Status { get; }
        public string Message { get; }
        public bool EvidenceCreated { get; }
        public bool EvidenceUpdated { get; }
    }

    /// <summary>Editor-only validation and deterministic Runtime Host plus Presentation materialization.</summary>
    public static class SceneProvidedLocalPlayerAuthoringUtility
    {
        public static SceneProvidedLocalPlayerAuthoringResult Validate(SceneProvidedLocalPlayerAuthoring authoring, bool logDiagnostics = true)
        {
            SceneProvidedLocalPlayerAuthoringResult result = ValidateCore(authoring, true);
            Record(authoring, result, logDiagnostics);
            return result;
        }

        public static SceneProvidedLocalPlayerAuthoringResult ApplyOrRebuild(SceneProvidedLocalPlayerAuthoring authoring, bool logDiagnostics = true, bool useUndo = true)
        {
            SceneProvidedLocalPlayerAuthoringResult preflight = ValidateMaterializationInputs(authoring);
            if (!preflight.Succeeded)
            {
                Record(authoring, preflight, logDiagnostics);
                return preflight;
            }

            SceneProvidedLocalPlayerAuthoringResult compositionPreflight =
                PreflightComposition(authoring);
            if (!compositionPreflight.Succeeded)
            {
                Record(authoring, compositionPreflight, logDiagnostics);
                return compositionPreflight;
            }

            SceneProvidedLocalPlayerAuthoringResult resolved = ResolveOrMaterialize(authoring, useUndo, out PlayerActorRuntimeHost runtimeHost, out GameObject presentation, out bool createdRuntimeHost, out bool createdPresentation);
            if (!resolved.Succeeded)
            {
                Record(authoring, resolved, logDiagnostics);
                return resolved;
            }

            ActorProfile profile = authoring.ActorProfile;
            bool evidenceCreated = EnsurePresentationEvidence(presentation, profile, useUndo, out bool evidenceUpdated);
            if (useUndo) Undo.RecordObject(authoring, "Apply Scene-Provided Local Player Evidence");
            authoring.EditorSetCompositionReferences(runtimeHost, presentation);
            authoring.EditorSetProfileEvidence(profile, profile.PresentationPrefab, BuildDiagnostic(profile, runtimeHost, presentation));
            EditorUtility.SetDirty(authoring);

            SceneProvidedLocalPlayerAuthoringResult validation = ValidateCore(authoring, true);
            SceneProvidedLocalPlayerAuthoringResult final = validation.Succeeded
                ? new SceneProvidedLocalPlayerAuthoringResult(true, SceneProvidedLocalPlayerAuthoringStatus.Valid, createdRuntimeHost || createdPresentation ? "Scene-Provided Local Player Runtime Host and selected Presentation were materialized under the exact Actor Mount and Presentation Mount." : "Scene-Provided Local Player Runtime Host and Presentation were preserved and typed Presentation evidence was refreshed.", evidenceCreated, evidenceUpdated)
                : validation;
            Record(authoring, final, logDiagnostics);
            return final;
        }

        private static SceneProvidedLocalPlayerAuthoringResult ValidateCore(SceneProvidedLocalPlayerAuthoring authoring, bool requireEvidence)
        {
            SceneProvidedLocalPlayerAuthoringResult inputs = ValidateMaterializationInputs(authoring);
            if (!inputs.Succeeded) return inputs;

            SceneProvidedLocalPlayerAuthoringResult compositionPreflight =
                PreflightComposition(authoring);
            if (!compositionPreflight.Succeeded)
            {
                return compositionPreflight;
            }

            if (authoring.ScenePlayerActorRuntimeHost == null || authoring.ScenePresentation == null)
            {
                return Failure(SceneProvidedLocalPlayerAuthoringStatus.MissingProfileEvidence, "Scene-Provided Local Player Runtime Host or Presentation is missing. Run Apply / Rebuild to materialize the selected composition.");
            }
            if (!ValidateSceneComposition(authoring, out string issue))
            {
                return Failure(SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence, issue);
            }
            if (!requireEvidence)
            {
                return new SceneProvidedLocalPlayerAuthoringResult(true, SceneProvidedLocalPlayerAuthoringStatus.Valid, "Scene-Provided Local Player Runtime Host and Presentation composition is valid.", false, false);
            }
            if (!authoring.TryValidateRuntimeEvidence(out issue))
            {
                return Failure(authoring.HasTypedActorEvidence ? SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence : SceneProvidedLocalPlayerAuthoringStatus.MissingProfileEvidence, issue);
            }
            ScenePlayerActorPresentationEvidence evidence = authoring.ScenePresentation.GetComponent<ScenePlayerActorPresentationEvidence>();
            if (evidence == null || !evidence.IsCompatibleWith(authoring.ActorProfile))
            {
                return Failure(SceneProvidedLocalPlayerAuthoringStatus.MissingProfileEvidence, "Scene-Provided Local Player Presentation lacks compatible serialized Presentation evidence. Run Apply / Rebuild.");
            }
            return new SceneProvidedLocalPlayerAuthoringResult(true, SceneProvidedLocalPlayerAuthoringStatus.Valid, "Scene-Provided Local Player authoring and Presentation evidence are valid.", false, false);
        }

        private static SceneProvidedLocalPlayerAuthoringResult ValidateMaterializationInputs(SceneProvidedLocalPlayerAuthoring authoring)
        {
            if (authoring == null) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidReferences, "Scene-Provided Local Player validation requires a target component.");
            LocalPlayerHostAuthoring host = authoring.LocalPlayerHost;
            if (host == null ||
                !ReferenceEquals(
                    authoring.GetComponentInParent<LocalPlayerHostAuthoring>(
                        true),
                    host))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Scene-Provided Local Player requires an explicit reference to the nearest ancestral Local Player Host that owns its hierarchy.");
            }
            if (authoring.PlayerSlotProfile == null)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidSlotProfile,
                    "Assign an explicit Player Slot Profile before Apply / Rebuild.");
            }

            if (!authoring.PlayerSlotProfile.TryGetPlayerSlotId(
                    out _,
                    out string slotIssue))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidSlotProfile,
                    slotIssue);
            }

            ActorProfile profile = authoring.ActorProfile;
            if (profile == null)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidActorProfile,
                    "Assign a Player Protagonist Actor Profile with an explicit Presentation Prefab.");
            }

            if (!profile.TryGetActorProfileId(out _, out string profileIssue) ||
                profile.ActorKind != ActorKind.Player ||
                profile.ActorRole != ActorRole.Protagonist ||
                profile.PresentationPrefab == null)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidActorProfile,
                    string.IsNullOrEmpty(profileIssue)
                        ? "Assign a Player Protagonist Actor Profile with an explicit Presentation Prefab."
                        : profileIssue);
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(profile.PresentationPrefab)) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidActorProfile, $"Actor Profile Presentation '{profile.PresentationPrefab.name}' must be a prefab asset.");
            if (profile.PresentationPrefab.GetComponentInChildren<PlayerInput>(true) != null || profile.PresentationPrefab.GetComponentInChildren<ActorDeclaration>(true) != null || profile.PresentationPrefab.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidActorProfile, "Actor Profile Presentation prefab must not contain PlayerInput, Actor declarations or Player Actor Runtime Host infrastructure.");
            if (host.PlayerInput == null)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Local Player Host is missing its explicit PlayerInput reference.");
            }

            if (host.ActorMount == null)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Local Player Host is missing its explicit Actor Mount.");
            }

            if (host.ActorMount == host.transform ||
                !host.ActorMount.IsChildOf(host.transform))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Local Player Host Actor Mount must be a child of the Local Player Host root.");
            }

            if (!host.HasPlayerActorRuntimeHostPrefab)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Local Player Host is missing its Player Actor Runtime Host Prefab.");
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(
                    host.PlayerActorRuntimeHostPrefab.gameObject))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Local Player Host Runtime Host reference must be a prefab composition.");
            }

            if (!host.PlayerActorRuntimeHostPrefab.TryValidateConfiguration(
                    out string runtimeHostIssue))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    runtimeHostIssue);
            }
            if (host.PlayerInput.gameObject != host.gameObject || host.GetComponentsInChildren<PlayerInput>(true).Length != 1 || host.ActorMount.GetComponentInChildren<PlayerInput>(true) != null) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidHost, "Local Player Host requires exactly one PlayerInput outside Actor Mount.");
            return new SceneProvidedLocalPlayerAuthoringResult(true, SceneProvidedLocalPlayerAuthoringStatus.Valid, "Scene-Provided Local Player materialization inputs are valid.", false, false);
        }

        private static SceneProvidedLocalPlayerAuthoringResult ResolveOrMaterialize(SceneProvidedLocalPlayerAuthoring authoring, bool useUndo, out PlayerActorRuntimeHost runtimeHost, out GameObject presentation, out bool createdRuntimeHost, out bool createdPresentation)
        {
            runtimeHost = authoring.ScenePlayerActorRuntimeHost;
            presentation = authoring.ScenePresentation;
            createdRuntimeHost = false;
            createdPresentation = false;
            Transform actorMount = authoring.LocalPlayerHost.ActorMount;

            if (runtimeHost == null)
            {
                PlayerActorRuntimeHost[] existing = actorMount.GetComponentsInChildren<PlayerActorRuntimeHost>(true);
                if (existing.Length > 1) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidHost, "Actor Mount contains multiple Player Actor Runtime Hosts. Apply / Rebuild will not guess ownership.");
                if (existing.Length == 1) runtimeHost = existing[0];
                else
                {
                    if (EditorUtility.IsPersistent(authoring.gameObject)) return Failure(SceneProvidedLocalPlayerAuthoringStatus.MissingProfileEvidence, "Runtime Host materialization cannot modify a prefab asset directly from the Project view.");
                    GameObject runtimeHostRoot = PrefabUtility.InstantiatePrefab(
                        authoring.LocalPlayerHost.PlayerActorRuntimeHostPrefab.gameObject,
                        actorMount) as GameObject;
                    runtimeHost = runtimeHostRoot != null
                        ? runtimeHostRoot.GetComponent<PlayerActorRuntimeHost>()
                        : null;
                    if (runtimeHost == null) return Failure(SceneProvidedLocalPlayerAuthoringStatus.MissingProfileEvidence, "Apply / Rebuild could not materialize the configured Player Actor Runtime Host prefab.");
                    if (useUndo) Undo.RegisterCreatedObjectUndo(runtimeHost.gameObject, "Create Scene-Provided Local Player Actor Runtime Host");
                    createdRuntimeHost = true;
                }
            }

            if (!ValidateRuntimeHostInstance(authoring, runtimeHost, out string runtimeHostIssue)) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidHost, runtimeHostIssue);
            Transform presentationMount = runtimeHost.PresentationMount;
            if (presentation == null)
            {
                if (presentationMount.childCount > 1) return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidHost, "Presentation Mount contains multiple children. Apply / Rebuild will not guess the selected Presentation.");
                if (presentationMount.childCount == 1) presentation = presentationMount.GetChild(0).gameObject;
                else
                {
                    presentation = PrefabUtility.InstantiatePrefab(authoring.ActorProfile.PresentationPrefab, presentationMount) as GameObject;
                    if (presentation == null) return Failure(SceneProvidedLocalPlayerAuthoringStatus.MissingProfileEvidence, "Apply / Rebuild could not materialize the selected Presentation prefab.");
                    if (useUndo) Undo.RegisterCreatedObjectUndo(presentation, "Create Scene-Provided Actor Presentation");
                    createdPresentation = true;
                }
            }

            if (!ValidatePresentationInstance(authoring, runtimeHost, presentation, out string presentationIssue)) return Failure(SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence, presentationIssue);
            return new SceneProvidedLocalPlayerAuthoringResult(true, SceneProvidedLocalPlayerAuthoringStatus.Valid, "Scene-Provided Local Player Runtime Host and Presentation composition resolved.", false, false);
        }

        private static SceneProvidedLocalPlayerAuthoringResult PreflightComposition(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            Transform actorMount = authoring.LocalPlayerHost.ActorMount;
            PlayerActorRuntimeHost[] runtimeHosts =
                actorMount.GetComponentsInChildren<PlayerActorRuntimeHost>(true);
            PlayerActorRuntimeHost runtimeHost =
                authoring.ScenePlayerActorRuntimeHost;

            if (runtimeHosts.Length > 1)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Actor Mount contains multiple Player Actor Runtime Hosts. Apply / Rebuild will not guess ownership.");
            }

            if (runtimeHost != null &&
                (runtimeHosts.Length != 1 || runtimeHosts[0] != runtimeHost))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "The authored Player Actor Runtime Host reference does not match the exact Runtime Host under Actor Mount.");
            }

            if (runtimeHost == null && runtimeHosts.Length == 1)
            {
                runtimeHost = runtimeHosts[0];
            }

            if (runtimeHost == null)
            {
                if (actorMount.childCount != 0)
                {
                    return Failure(
                        SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                        "Actor Mount contains external content but no Player Actor Runtime Host. Apply / Rebuild will not add composition beside unknown content.");
                }

                if (authoring.ScenePresentation != null)
                {
                    return Failure(
                        SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence,
                        "Scene Presentation is assigned without a Player Actor Runtime Host. Assign the matching Runtime Host or clear the conflicting reference before Apply / Rebuild.");
                }

                return new SceneProvidedLocalPlayerAuthoringResult(
                    true,
                    SceneProvidedLocalPlayerAuthoringStatus.Valid,
                    "Actor Mount is empty and ready for deterministic Runtime Host materialization.",
                    false,
                    false);
            }

            if (!ValidateRuntimeHostInstance(authoring, runtimeHost, out string runtimeHostIssue))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    runtimeHostIssue);
            }

            if (actorMount.childCount != 1 ||
                actorMount.GetChild(0) != runtimeHost.transform)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Actor Mount contains content outside the exact Player Actor Runtime Host. Apply / Rebuild will not modify an ambiguous composition.");
            }

            Transform presentationMount = runtimeHost.PresentationMount;
            GameObject presentation = authoring.ScenePresentation;
            if (presentation != null)
            {
                if (presentationMount.childCount != 1 ||
                    presentationMount.GetChild(0).gameObject != presentation)
                {
                    return Failure(
                        SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence,
                        "The authored Scene Presentation reference does not match the exact child of Presentation Mount.");
                }
            }
            else if (presentationMount.childCount > 1)
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence,
                    "Presentation Mount contains multiple children. Apply / Rebuild will not guess the selected Presentation.");
            }
            else if (presentationMount.childCount == 1)
            {
                presentation = presentationMount.GetChild(0).gameObject;
            }

            if (presentation == null)
            {
                return new SceneProvidedLocalPlayerAuthoringResult(
                    true,
                    SceneProvidedLocalPlayerAuthoringStatus.Valid,
                    "Runtime Host is valid and its Presentation Mount is empty and ready for deterministic Presentation materialization.",
                    false,
                    false);
            }

            if (!ValidatePresentationInstance(
                    authoring,
                    runtimeHost,
                    presentation,
                    out string presentationIssue))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.IncompatibleProfileEvidence,
                    presentationIssue);
            }

            return new SceneProvidedLocalPlayerAuthoringResult(
                true,
                SceneProvidedLocalPlayerAuthoringStatus.Valid,
                "Existing Runtime Host and Presentation match the selected Scene-Provided composition.",
                false,
                false);
        }

        private static bool ValidateSceneComposition(SceneProvidedLocalPlayerAuthoring authoring, out string issue)
        {
            return ValidateRuntimeHostInstance(authoring, authoring.ScenePlayerActorRuntimeHost, out issue) && ValidatePresentationInstance(authoring, authoring.ScenePlayerActorRuntimeHost, authoring.ScenePresentation, out issue);
        }

        private static bool ValidateRuntimeHostInstance(SceneProvidedLocalPlayerAuthoring authoring, PlayerActorRuntimeHost runtimeHost, out string issue)
        {
            issue = string.Empty;
            if (runtimeHost == null || runtimeHost.transform.parent != authoring.LocalPlayerHost.ActorMount || !runtimeHost.TryValidateConfiguration(out issue)) return false;
            GameObject source = ResolveSourcePrefab(runtimeHost.gameObject);
            if (!AreSamePrefabAsset(source, authoring.LocalPlayerHost.PlayerActorRuntimeHostPrefab.gameObject))
            {
            issue = "Scene-Provided Local Player Actor Runtime Host does not match the exact Runtime Host prefab provided by the Local Player Host.";
                return false;
            }
            return true;
        }

        private static bool ValidatePresentationInstance(SceneProvidedLocalPlayerAuthoring authoring, PlayerActorRuntimeHost runtimeHost, GameObject presentation, out string issue)
        {
            issue = string.Empty;
            if (runtimeHost == null || presentation == null || presentation.transform.parent != runtimeHost.PresentationMount) { issue = "Scene-Provided Local Player Presentation must be a direct child of the exact Player Actor Runtime Host Presentation Mount."; return false; }
            if (presentation.GetComponentInChildren<PlayerInput>(true) != null || presentation.GetComponentInChildren<ActorDeclaration>(true) != null || presentation.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null) { issue = "Scene-Provided Local Player Presentation must not contain PlayerInput, Actor declarations or Player Actor Runtime Host infrastructure."; return false; }
            if (!AreSamePrefabAsset(ResolveSourcePrefab(presentation), authoring.ActorProfile.PresentationPrefab)) { issue = "Scene-Provided Local Player Presentation prefab source does not match the selected Actor Profile Presentation prefab. Apply / Rebuild will not silently replace conflicting authored content."; return false; }
            return true;
        }

        private static bool EnsurePresentationEvidence(GameObject presentation, ActorProfile profile, bool useUndo, out bool updated)
        {
            ScenePlayerActorPresentationEvidence evidence = presentation.GetComponent<ScenePlayerActorPresentationEvidence>();
            bool created = evidence == null;
            if (created)
            {
                evidence = useUndo ? Undo.AddComponent<ScenePlayerActorPresentationEvidence>(presentation) : presentation.AddComponent<ScenePlayerActorPresentationEvidence>();
            }
            updated = created || !evidence.IsCompatibleWith(profile);
            if (updated)
            {
                if (useUndo) Undo.RecordObject(evidence, "Apply Scene-Provided Local Player Presentation Evidence");
                evidence.EditorSetEvidence(profile, profile.PresentationPrefab, $"Profile='{profile.name}' presentation='{presentation.name}'.");
                EditorUtility.SetDirty(evidence);
            }
            return created;
        }

        private static string BuildDiagnostic(ActorProfile profile, PlayerActorRuntimeHost runtimeHost, GameObject presentation) => $"Profile='{profile.name}' runtimeHost='{runtimeHost.name}' presentation='{presentation.name}'.";

        private static GameObject ResolveSourcePrefab(GameObject instance)
        {
            if (instance == null) return null;
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(instance);
            GameObject source = root != null ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(root) : PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            return source != null ? source.transform.root.gameObject : null;
        }

        private static bool AreSamePrefabAsset(GameObject first, GameObject second)
        {
            if (first == null || second == null) return first == second;
            if (first == second) return true;
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(first, out string firstGuid, out long firstId) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(second, out string secondGuid, out long secondId) && string.Equals(firstGuid, secondGuid, StringComparison.Ordinal) && firstId == secondId;
        }

        private static SceneProvidedLocalPlayerAuthoringResult Failure(SceneProvidedLocalPlayerAuthoringStatus status, string message) => new SceneProvidedLocalPlayerAuthoringResult(false, status, message, false, false);

        private static void Record(SceneProvidedLocalPlayerAuthoring authoring, SceneProvidedLocalPlayerAuthoringResult result, bool logDiagnostics)
        {
            if (authoring != null) { authoring.EditorSetAuthoringResult(result.Status, result.Message); EditorUtility.SetDirty(authoring); }
            if (!logDiagnostics) return;
            FrameworkLogger.Create(typeof(SceneProvidedLocalPlayerAuthoringUtility)).Info($"[Immersive.Framework][SceneProvidedPlayer] status='{result.Status}' succeeded='{result.Succeeded}' createdEvidence='{result.EvidenceCreated}' updatedEvidence='{result.EvidenceUpdated}' diagnostic='{result.Message}'.");
        }
    }
}
