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
    /// Editor-only validation and internal evidence materialization for one
    /// Scene-Provided Player composer.
    /// </summary>
    public static class SceneLocalPlayerAdmissionAuthoringUtility
    {
        public static SceneLocalPlayerAdmissionAuthoringResult Validate(
            SceneLocalPlayerAdmissionAuthoring authoring,
            bool logDiagnostics = true)
        {
            SceneLocalPlayerAdmissionAuthoringResult result =
                ValidateCore(
                    authoring,
                    requireEvidence: true);
            Record(
                authoring,
                result,
                logDiagnostics);
            return result;
        }

        public static SceneLocalPlayerAdmissionAuthoringResult ApplyOrRebuild(
            SceneLocalPlayerAdmissionAuthoring authoring,
            bool logDiagnostics = true,
            bool useUndo = true)
        {
            SceneLocalPlayerAdmissionAuthoringResult preflight =
                ValidateCore(
                    authoring,
                    requireEvidence: false);
            if (!preflight.Succeeded)
            {
                Record(
                    authoring,
                    preflight,
                    logDiagnostics);
                return preflight;
            }

            PlayerActorDeclaration actor =
                authoring.SceneLogicalPlayerActor;
            ActorProfile actorProfile =
                authoring.ActorProfile;
            GameObject profilePrefab =
                actorProfile.LogicalActorHostPrefab;
            GameObject sourcePrefab =
                ResolveSourcePrefab(actor.gameObject);

            if (sourcePrefab == null)
            {
                var result = Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    "Scene Logical Player Actor is not connected to a prefab source. Author the Actor from the selected Actor Profile Logical Actor Host prefab before Apply / Rebuild.");
                Record(
                    authoring,
                    result,
                    logDiagnostics);
                return result;
            }

            if (!AreSamePrefabAsset(
                    sourcePrefab,
                    profilePrefab))
            {
                var result = Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence,
                    $"Scene Logical Player Actor prefab source '{sourcePrefab.name}' does not match Actor Profile '{actorProfile.name}' Logical Actor Host prefab '{profilePrefab.name}'.");
                Record(
                    authoring,
                    result,
                    logDiagnostics);
                return result;
            }

            bool created =
                !authoring.HasTypedActorEvidence;
            bool updated =
                created ||
                !authoring.IsTypedActorEvidenceCompatibleWith(
                    actorProfile) ||
                !string.Equals(
                    authoring.EvidenceDiagnostic,
                    BuildDiagnostic(
                        actorProfile,
                        profilePrefab,
                        actor),
                    StringComparison.Ordinal);

            if (useUndo)
            {
                Undo.RecordObject(
                    authoring,
                    "Apply Scene-Provided Player Evidence");
            }

            authoring.EditorSetProfileEvidence(
                actorProfile,
                profilePrefab,
                BuildDiagnostic(
                    actorProfile,
                    profilePrefab,
                    actor));
            EditorUtility.SetDirty(authoring);

            SceneLocalPlayerAdmissionAuthoringResult validation =
                ValidateCore(
                    authoring,
                    requireEvidence: true);

            var final =
                validation.Succeeded
                    ? new SceneLocalPlayerAdmissionAuthoringResult(
                        true,
                        SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                        "Scene-Provided Player authoring is valid. Typed Actor Profile evidence is stored in the composer; no runtime identity or gameplay was started.",
                        created,
                        updated)
                    : validation;

            Record(
                authoring,
                final,
                logDiagnostics);
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
                    "Scene-Provided Player validation requires a target component.");
            }

            if (!authoring.HasCompleteReferences)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidReferences,
                    "Assign Player Slot Profile, Actor Profile and Scene Logical Player Actor. Local Player Host is resolved from this same GameObject.");
            }

            if (authoring.LocalPlayerHost.gameObject != authoring.gameObject)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Scene-Provided Player composer and Local Player Host must exist on the same GameObject.");
            }

            if (!authoring.PlayerSlotProfile.TryGetPlayerSlotId(
                    out _,
                    out string slotIssue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidSlotProfile,
                    slotIssue);
            }

            ActorProfile profile =
                authoring.ActorProfile;
            if (!profile.TryGetActorProfileId(
                    out _,
                    out string profileIssue) ||
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

            if (!authoring.LocalPlayerHost
                    .TryValidateAdmissionConfiguration(
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
                    "Scene-Provided Player references and hierarchy are valid for evidence materialization.",
                    false,
                    false);
            }

            if (!authoring.TryValidateRuntimeEvidence(
                    out string runtimeIssue))
            {
                SceneLocalPlayerAdmissionAuthoringStatus status =
                    authoring.HasTypedActorEvidence
                        ? SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence
                        : SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence;
                return Failure(
                    status,
                    runtimeIssue);
            }

            return new SceneLocalPlayerAdmissionAuthoringResult(
                true,
                SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                "Scene-Provided Player authoring and internal profile evidence are valid.",
                false,
                false);
        }

        private static string BuildDiagnostic(
            ActorProfile actorProfile,
            GameObject profilePrefab,
            PlayerActorDeclaration actor)
        {
            return
                $"Profile='{actorProfile.name}' sourcePrefab='{profilePrefab.name}' actor='{actor.name}'.";
        }

        private static GameObject ResolveSourcePrefab(
            GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            // For a nested Actor prefab inside Player_SceneProvided, resolving the
            // outer corresponding source returns the composed Player prefab root.
            // The nearest prefab instance root preserves the authored Actor prefab
            // boundary that ActorProfile.LogicalActorHostPrefab must match.
            GameObject nearestInstanceRoot =
                PrefabUtility.GetNearestPrefabInstanceRoot(instance);

            if (nearestInstanceRoot != null)
            {
                GameObject originalNestedSource =
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                        nearestInstanceRoot);
                if (originalNestedSource != null)
                {
                    return originalNestedSource.transform.root.gameObject;
                }

                GameObject nestedSource =
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        nearestInstanceRoot);
                if (nestedSource != null)
                {
                    return nestedSource.transform.root.gameObject;
                }
            }

            GameObject originalSource =
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                    instance);
            if (originalSource != null)
            {
                return originalSource.transform.root.gameObject;
            }

            GameObject source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    instance);
            return source != null
                ? source.transform.root.gameObject
                : null;
        }

        private static bool AreSamePrefabAsset(
            GameObject first,
            GameObject second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }

            if (first == second)
            {
                return true;
            }

            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                       first,
                       out string firstGuid,
                       out long firstLocalId) &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                       second,
                       out string secondGuid,
                       out long secondLocalId) &&
                   string.Equals(
                       firstGuid,
                       secondGuid,
                       StringComparison.Ordinal) &&
                   firstLocalId == secondLocalId;
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
                authoring.EditorSetAuthoringResult(
                    result.Status,
                    result.Message);
                EditorUtility.SetDirty(authoring);
            }

            if (!logDiagnostics)
            {
                return;
            }

            string message =
                $"[Immersive.Framework][SceneProvidedPlayer] status='{result.Status}' succeeded='{result.Succeeded}' createdEvidence='{result.EvidenceCreated}' updatedEvidence='{result.EvidenceUpdated}' diagnostic='{result.Message}'.";

            var logger =
                FrameworkLogger.Create(
                    typeof(SceneLocalPlayerAdmissionAuthoringUtility));
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
