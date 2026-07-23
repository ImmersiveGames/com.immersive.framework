using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    public readonly struct SceneLocalPlayerAdmissionAuthoringResult
    {
        public SceneLocalPlayerAdmissionAuthoringResult(
            bool succeeded,
            SceneLocalPlayerAdmissionAuthoringStatus status,
            string message,
            bool evidenceCreated,
            bool evidenceUpdated)
        {
            Succeeded = succeeded;
            Status = status;
            Message = message ?? string.Empty;
            EvidenceCreated = evidenceCreated;
            EvidenceUpdated = evidenceUpdated;
        }

        public bool Succeeded { get; }
        public SceneLocalPlayerAdmissionAuthoringStatus Status { get; }
        public string Message { get; }
        public bool EvidenceCreated { get; }
        public bool EvidenceUpdated { get; }
    }

    /// <summary>
    /// Editor-only validation and evidence materialization for Scene Local Player Admission.
    /// This utility never reserves a Slot, assigns runtime identity or starts gameplay.
    /// </summary>
    public static class SceneLocalPlayerAdmissionAuthoringUtility
    {
        public static SceneLocalPlayerAdmissionAuthoringResult Validate(
            SceneLocalPlayerAdmissionAuthoring authoring,
            bool logDiagnostics = true)
        {
            SceneLocalPlayerAdmissionAuthoringResult result =
                ValidateCore(authoring, requireEvidence: true);
            Record(authoring, result, logDiagnostics);
            return result;
        }

        public static SceneLocalPlayerAdmissionAuthoringResult ApplyOrRebuild(
            SceneLocalPlayerAdmissionAuthoring authoring,
            bool logDiagnostics = true,
            bool useUndo = true)
        {
            SceneLocalPlayerAdmissionAuthoringResult preflight =
                ValidateCore(authoring, requireEvidence: false);
            if (!preflight.Succeeded)
            {
                Record(authoring, preflight, logDiagnostics);
                return preflight;
            }

            PlayerActorDeclaration actor = authoring.SceneLogicalPlayerActor;
            ActorProfile actorProfile = authoring.ActorProfile;
            GameObject profilePrefab = actorProfile.LogicalActorHostPrefab;
            GameObject sourcePrefab = ResolveSourcePrefab(actor.gameObject);
            if (sourcePrefab == null)
            {
                var result = Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    "Scene Logical Player Actor is not connected to a prefab source. Author the Actor from the selected Actor Profile Logical Actor Host prefab before Apply / Rebuild.");
                Record(authoring, result, logDiagnostics);
                return result;
            }

            if (!ReferenceEquals(sourcePrefab, profilePrefab))
            {
                var result = Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence,
                    $"Scene Logical Player Actor prefab source '{sourcePrefab.name}' does not match Actor Profile '{actorProfile.name}' Logical Actor Host prefab '{profilePrefab.name}'.");
                Record(authoring, result, logDiagnostics);
                return result;
            }

            SceneLogicalPlayerActorEvidence evidence =
                actor.GetComponent<SceneLogicalPlayerActorEvidence>();
            bool created = evidence == null;
            if (created)
            {
                evidence = useUndo
                    ? Undo.AddComponent<SceneLogicalPlayerActorEvidence>(actor.gameObject)
                    : actor.gameObject.AddComponent<SceneLogicalPlayerActorEvidence>();
            }
            else if (useUndo)
            {
                Undo.RecordObject(evidence, "Apply Scene Local Player Actor Evidence");
            }

            string diagnostic =
                $"Profile='{actorProfile.name}' sourcePrefab='{profilePrefab.name}' actor='{actor.name}'.";
            bool updated = created ||
                !ReferenceEquals(evidence.ActorProfile, actorProfile) ||
                !ReferenceEquals(evidence.LogicalActorHostPrefab, profilePrefab) ||
                !string.Equals(evidence.AuthoringDiagnostic, diagnostic, StringComparison.Ordinal);

            evidence.EditorSetEvidence(actorProfile, profilePrefab, diagnostic);
            EditorUtility.SetDirty(evidence);
            EditorUtility.SetDirty(actor.gameObject);

            SceneLocalPlayerAdmissionAuthoringResult validation =
                ValidateCore(authoring, requireEvidence: true);
            var final = validation.Succeeded
                ? new SceneLocalPlayerAdmissionAuthoringResult(
                    true,
                    SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                    "Scene Local Player Admission authoring is valid. Typed profile evidence was materialized without assigning runtime identity or starting admission.",
                    created,
                    updated)
                : validation;
            Record(authoring, final, logDiagnostics);
            return final;
        }

        private static SceneLocalPlayerAdmissionAuthoringResult ValidateCore(
            SceneLocalPlayerAdmissionAuthoring authoring,
            bool requireEvidence)
        {
            if (authoring == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidReferences,
                    "Scene Local Player Admission validation requires a target component.");
            }

            if (!authoring.HasCompleteReferences)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidReferences,
                    "Assign Player Slot Profile, Local Player Host, Actor Profile and Scene Logical Player Actor.");
            }

            if (!authoring.PlayerSlotProfile.TryGetPlayerSlotId(out _, out string slotIssue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidSlotProfile,
                    slotIssue);
            }

            ActorProfile profile = authoring.ActorProfile;
            if (!profile.TryGetActorProfileId(out _, out string profileIssue) ||
                profile.ActorKind != ActorKind.Player ||
                profile.ActorRole != ActorRole.Protagonist ||
                profile.LogicalActorHostPrefab == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidActorProfile,
                    string.IsNullOrWhiteSpace(profileIssue)
                        ? $"Actor Profile '{profile.name}' must define a Player Protagonist Logical Actor Host prefab."
                        : profileIssue);
            }

            if (!authoring.LocalPlayerHost.TryValidateAdmissionConfiguration(
                    authoring.SceneLogicalPlayerActor,
                    allowExistingLogicalActor: true,
                    out string hostIssue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    hostIssue);
            }

            if (!requireEvidence)
            {
                return new SceneLocalPlayerAdmissionAuthoringResult(
                    true,
                    SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                    "Scene Local Player Admission references and hierarchy are valid for evidence materialization.",
                    false,
                    false);
            }

            if (!authoring.TryValidateRuntimeEvidence(out string runtimeIssue))
            {
                SceneLocalPlayerAdmissionAuthoringStatus status =
                    authoring.SceneLogicalPlayerActor.GetComponent<SceneLogicalPlayerActorEvidence>() == null
                        ? SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence
                        : SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence;
                return Failure(status, runtimeIssue);
            }

            return new SceneLocalPlayerAdmissionAuthoringResult(
                true,
                SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                "Scene Local Player Admission authoring and serialized profile evidence are valid.",
                false,
                false);
        }

        private static GameObject ResolveSourcePrefab(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (source == null)
            {
                return null;
            }

            return source.transform.root.gameObject;
        }

        private static SceneLocalPlayerAdmissionAuthoringResult Failure(
            SceneLocalPlayerAdmissionAuthoringStatus status,
            string message)
        {
            return new SceneLocalPlayerAdmissionAuthoringResult(
                false,
                status,
                message,
                false,
                false);
        }

        private static void Record(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionAuthoringResult result,
            bool logDiagnostics)
        {
            if (authoring != null)
            {
                authoring.EditorSetAuthoringResult(result.Status, result.Message);
                EditorUtility.SetDirty(authoring);
            }

            if (!logDiagnostics)
            {
                return;
            }

            string message =
                $"[Immersive.Framework][SceneLocalPlayerAdmission] status='{result.Status}' succeeded='{result.Succeeded}' createdEvidence='{result.EvidenceCreated}' updatedEvidence='{result.EvidenceUpdated}' diagnostic='{result.Message}'.";
            var logger = FrameworkLogger.Create(typeof(SceneLocalPlayerAdmissionAuthoringUtility));
            if (result.Succeeded)
            {
                logger.Info(message);
            }
            else
            {
                logger.Warning(message);
            }
        }
    }
}
