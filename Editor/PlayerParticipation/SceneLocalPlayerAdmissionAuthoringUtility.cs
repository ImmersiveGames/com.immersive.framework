using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// Editor-only validation and deterministic Scene Actor / internal evidence
    /// materialization for one Scene-Provided Player composer.
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
                ValidateMaterializationInputs(authoring);
            if (!preflight.Succeeded)
            {
                Record(
                    authoring,
                    preflight,
                    logDiagnostics);
                return preflight;
            }

            SceneLocalPlayerAdmissionAuthoringResult actorResolution =
                ResolveOrMaterializeSceneActor(
                    authoring,
                    useUndo,
                    out PlayerActorDeclaration actor,
                    out bool sceneActorCreated);
            if (!actorResolution.Succeeded)
            {
                Record(
                    authoring,
                    actorResolution,
                    logDiagnostics);
                return actorResolution;
            }

            ActorProfile actorProfile =
                authoring.ActorProfile;
            GameObject profilePrefab =
                actorProfile.LogicalActorHostPrefab;

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

            string successMessage =
                sceneActorCreated
                    ? "Scene-Provided Player authoring is valid. The Actor Profile Logical Actor Host prefab was materialized under Actor Mount and typed Actor Profile evidence is stored; no runtime identity or gameplay was started."
                    : "Scene-Provided Player authoring is valid. The matching Scene Actor instance was preserved and typed Actor Profile evidence is stored; no runtime identity or gameplay was started.";

            var final =
                validation.Succeeded
                    ? new SceneLocalPlayerAdmissionAuthoringResult(
                        true,
                        SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                        successMessage,
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
            SceneLocalPlayerAdmissionAuthoringResult inputs =
                ValidateMaterializationInputs(authoring);
            if (!inputs.Succeeded)
            {
                return inputs;
            }

            PlayerActorDeclaration actor =
                authoring.SceneLogicalPlayerActor;
            if (actor == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    "Scene Actor is not materialized yet. Run Apply / Rebuild to instantiate the selected Actor Profile Logical Actor Host prefab under this Host's Actor Mount.");
            }

            SceneLocalPlayerAdmissionAuthoringResult actorValidation =
                ValidateSceneActorInstance(
                    authoring,
                    actor);
            if (!actorValidation.Succeeded)
            {
                return actorValidation;
            }

            if (!requireEvidence)
            {
                return new SceneLocalPlayerAdmissionAuthoringResult(
                    true,
                    SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                    "Scene-Provided Player references, hierarchy and Scene Actor prefab provenance are valid for evidence materialization.",
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

        private static SceneLocalPlayerAdmissionAuthoringResult
            ValidateMaterializationInputs(
                SceneLocalPlayerAdmissionAuthoring authoring)
        {
            if (authoring == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidReferences,
                    "Scene-Provided Player validation requires a target component.");
            }

            LocalPlayerHostAuthoring host =
                authoring.LocalPlayerHost;
            if (host == null || host.gameObject != authoring.gameObject)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Scene-Provided Player composer and Local Player Host must exist on the same GameObject.");
            }

            if (authoring.PlayerSlotProfile == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidReferences,
                    "Assign an explicit Player Slot Profile before Apply / Rebuild.");
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
            if (profile == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidReferences,
                    "Assign an Actor Profile before Apply / Rebuild. The Actor Profile is the single prefab source authority for the Scene Actor.");
            }

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

            GameObject profilePrefab =
                profile.LogicalActorHostPrefab;
            if (!PrefabUtility.IsPartOfPrefabAsset(profilePrefab))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidActorProfile,
                    $"Actor Profile '{profile.name}' Logical Actor Host '{profilePrefab.name}' must be a prefab asset before Apply / Rebuild can materialize it.");
            }

            PlayerActorDeclaration[] profilePlayerDeclarations =
                profilePrefab.GetComponentsInChildren<PlayerActorDeclaration>(true);
            ActorDeclaration[] profileActorDeclarations =
                profilePrefab.GetComponentsInChildren<ActorDeclaration>(true);
            if (profilePlayerDeclarations.Length != 1 ||
                profileActorDeclarations.Length != 1 ||
                profileActorDeclarations[0] != profilePlayerDeclarations[0])
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidActorProfile,
                    $"Actor Profile '{profile.name}' Logical Actor Host prefab '{profilePrefab.name}' must contain exactly one canonical PlayerActorDeclaration and no additional ActorDeclaration before it can be materialized.");
            }

            PlayerInput[] profilePlayerInputs =
                profilePrefab.GetComponentsInChildren<PlayerInput>(true);
            if (profilePlayerInputs.Length != 0)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidActorProfile,
                    $"Actor Profile '{profile.name}' Logical Actor Host prefab '{profilePrefab.name}' must not contain PlayerInput. PlayerInput belongs to the Local Player Host.");
            }

            if (host.PlayerInput == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Local Player Host requires an explicit PlayerInput reference before Scene Actor materialization.");
            }

            if (host.PlayerInput.gameObject != host.gameObject)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Local Player Host PlayerInput must exist on the same GameObject as LocalPlayerHostAuthoring.");
            }

            PlayerInput[] hostPlayerInputs =
                host.GetComponentsInChildren<PlayerInput>(true);
            if (hostPlayerInputs.Length != 1 ||
                hostPlayerInputs[0] != host.PlayerInput)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    $"Local Player Host requires exactly one PlayerInput in its hierarchy. Found '{hostPlayerInputs.Length}'.");
            }

            if (host.ActorMount == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Local Player Host requires an explicit Actor Mount child transform before Scene Actor materialization.");
            }

            if (host.ActorMount == host.transform ||
                !host.ActorMount.IsChildOf(host.transform))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Local Player Host Actor Mount must be a child of the technical host root.");
            }

            if (host.ActorMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Local Player Host Actor Mount must not contain a second PlayerInput.");
            }

            return new SceneLocalPlayerAdmissionAuthoringResult(
                true,
                SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                "Scene-Provided Player materialization inputs are valid.",
                false,
                false);
        }

        private static SceneLocalPlayerAdmissionAuthoringResult
            ResolveOrMaterializeSceneActor(
                SceneLocalPlayerAdmissionAuthoring authoring,
                bool useUndo,
                out PlayerActorDeclaration actor,
                out bool actorCreated)
        {
            actor = authoring.SceneLogicalPlayerActor;
            actorCreated = false;

            if (actor != null)
            {
                return ValidateSceneActorInstance(
                    authoring,
                    actor);
            }

            Transform actorMount =
                authoring.LocalPlayerHost.ActorMount;
            PlayerActorDeclaration[] existingPlayers =
                actorMount.GetComponentsInChildren<PlayerActorDeclaration>(true);
            ActorDeclaration[] existingActors =
                actorMount.GetComponentsInChildren<ActorDeclaration>(true);

            if (existingPlayers.Length > 1 || existingActors.Length > 1)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    $"Actor Mount contains multiple Actor declarations. Apply / Rebuild will not guess which Scene Actor to own. playerDeclarations='{existingPlayers.Length}' actorDeclarations='{existingActors.Length}'.");
            }

            if (existingPlayers.Length == 1)
            {
                PlayerActorDeclaration existing =
                    existingPlayers[0];
                SceneLocalPlayerAdmissionAuthoringResult existingValidation =
                    ValidateSceneActorInstance(
                        authoring,
                        existing);
                if (!existingValidation.Succeeded)
                {
                    return existingValidation;
                }

                AssignSceneActorReference(
                    authoring,
                    existing,
                    useUndo);
                actor = existing;
                return new SceneLocalPlayerAdmissionAuthoringResult(
                    true,
                    SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                    "Existing matching Scene Actor prefab instance was bound to the composer.",
                    false,
                    false);
            }

            if (existingActors.Length != 0)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    "Actor Mount already contains a non-Player Actor declaration. Apply / Rebuild will not replace or destroy conflicting authored content.");
            }

            if (EditorUtility.IsPersistent(authoring.gameObject))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    "Scene Actor materialization cannot modify a prefab asset directly from the Project view. Open the Player prefab in Prefab Mode or edit a scene instance, then run Apply / Rebuild again.");
            }

            GameObject profilePrefab =
                authoring.ActorProfile.LogicalActorHostPrefab;
            GameObject instanceRoot;
            try
            {
                instanceRoot =
                    PrefabUtility.InstantiatePrefab(
                        profilePrefab,
                        actorMount) as GameObject;
            }
            catch (Exception exception)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    $"Apply / Rebuild could not materialize Logical Actor Host prefab '{profilePrefab.name}' under Actor Mount '{actorMount.name}'. {exception.Message}");
            }

            if (instanceRoot == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    $"Apply / Rebuild could not materialize Logical Actor Host prefab '{profilePrefab.name}' under Actor Mount '{actorMount.name}'.");
            }

            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(
                    instanceRoot,
                    "Create Scene-Provided Player Actor");
            }

            PlayerActorDeclaration[] createdPlayers =
                instanceRoot.GetComponentsInChildren<PlayerActorDeclaration>(true);
            ActorDeclaration[] createdActors =
                instanceRoot.GetComponentsInChildren<ActorDeclaration>(true);
            if (createdPlayers.Length != 1 ||
                createdActors.Length != 1 ||
                createdActors[0] != createdPlayers[0])
            {
                DestroyCreatedInstance(
                    instanceRoot,
                    useUndo);
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidActorProfile,
                    $"Materialized Logical Actor Host prefab '{profilePrefab.name}' did not produce exactly one canonical PlayerActorDeclaration. The partial instance was removed.");
            }

            PlayerActorDeclaration createdActor =
                createdPlayers[0];
            SceneLocalPlayerAdmissionAuthoringResult createdValidation =
                ValidateSceneActorInstance(
                    authoring,
                    createdActor);
            if (!createdValidation.Succeeded)
            {
                DestroyCreatedInstance(
                    instanceRoot,
                    useUndo);
                return createdValidation;
            }

            AssignSceneActorReference(
                authoring,
                createdActor,
                useUndo);
            actor = createdActor;
            actorCreated = true;

            return new SceneLocalPlayerAdmissionAuthoringResult(
                true,
                SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                $"Logical Actor Host prefab '{profilePrefab.name}' was materialized under Actor Mount '{actorMount.name}' and bound as the Scene Actor.",
                false,
                false);
        }

        private static SceneLocalPlayerAdmissionAuthoringResult
            ValidateSceneActorInstance(
                SceneLocalPlayerAdmissionAuthoring authoring,
                PlayerActorDeclaration actor)
        {
            if (actor == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    "Scene Actor is missing. Run Apply / Rebuild to materialize it from the selected Actor Profile.");
            }

            if (!authoring.LocalPlayerHost
                    .TryValidateAdmissionConfiguration(
                        actor,
                        allowExistingLogicalActor: true,
                        out string hostIssue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.InvalidHost,
                    hostIssue);
            }

            GameObject profilePrefab =
                authoring.ActorProfile.LogicalActorHostPrefab;
            GameObject sourcePrefab =
                ResolveSourcePrefab(actor.gameObject);

            if (sourcePrefab == null)
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.MissingProfileEvidence,
                    "Scene Actor is not connected to a prefab source. Restore its prefab connection, or remove the conflicting Actor and run Apply / Rebuild to materialize the selected Actor Profile Logical Actor Host prefab.");
            }

            if (!AreSamePrefabAsset(
                    sourcePrefab,
                    profilePrefab))
            {
                return Failure(
                    SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence,
                    $"Scene Actor prefab source '{sourcePrefab.name}' does not match Actor Profile '{authoring.ActorProfile.name}' Logical Actor Host prefab '{profilePrefab.name}'. Apply / Rebuild will not silently replace conflicting authored content.");
            }

            return new SceneLocalPlayerAdmissionAuthoringResult(
                true,
                SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                "Scene Actor instance matches the selected Actor Profile prefab authority.",
                false,
                false);
        }

        private static void AssignSceneActorReference(
            SceneLocalPlayerAdmissionAuthoring authoring,
            PlayerActorDeclaration actor,
            bool useUndo)
        {
            if (authoring.SceneLogicalPlayerActor == actor)
            {
                return;
            }

            if (useUndo)
            {
                Undo.RecordObject(
                    authoring,
                    "Bind Scene-Provided Player Actor");
            }

            var serialized =
                new SerializedObject(authoring);
            serialized.Update();
            SerializedProperty property =
                serialized.FindProperty("sceneLogicalPlayerActor");
            property.objectReferenceValue = actor;

            if (useUndo)
            {
                serialized.ApplyModifiedProperties();
            }
            else
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(authoring);
        }

        private static void DestroyCreatedInstance(
            GameObject instanceRoot,
            bool useUndo)
        {
            if (instanceRoot == null)
            {
                return;
            }

            if (useUndo)
            {
                Undo.DestroyObjectImmediate(instanceRoot);
                return;
            }

            UnityEngine.Object.DestroyImmediate(instanceRoot);
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
