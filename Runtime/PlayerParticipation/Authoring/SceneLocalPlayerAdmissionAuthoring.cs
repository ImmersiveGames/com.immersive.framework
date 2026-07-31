using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing composer for one local Player that already exists in a scene.
    /// The Local Player Host is resolved from this same GameObject; the selected Logical
    /// Player Actor remains explicit under that Host's Actor Mount.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LocalPlayerHostAuthoring))]
    [AddComponentMenu("Immersive Framework/Player/Scene-Provided Player Composer")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable Scene-Provided local Player product surface. Manager-Provisioned and Session-Persistent remain Experimental.")]
    public sealed class SceneLocalPlayerAdmissionAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Exact configured Player Slot to admit. Runtime never allocates a fallback Slot.")]
        private PlayerSlotProfile playerSlotProfile;

        [SerializeField]
        [Tooltip("Exact Actor Profile selected for this Scene-Provided Player.")]
        private ActorProfile actorProfile;

        [SerializeField]
        [Tooltip("Exact authored Logical Player Actor under this Host's Actor Mount.")]
        private PlayerActorDeclaration sceneLogicalPlayerActor;

        [SerializeField]
        private SceneLocalPlayerAdmissionTiming admissionTiming =
            SceneLocalPlayerAdmissionTiming.OnActivityEnter;

        [SerializeField, HideInInspector]
        private ActorProfile evidenceActorProfile;

        [SerializeField, HideInInspector]
        private GameObject evidenceLogicalActorHostPrefab;

        [SerializeField, HideInInspector]
        private string evidenceDiagnostic = string.Empty;

        [SerializeField, HideInInspector]
        private SceneLocalPlayerAdmissionAuthoringStatus lastAuthoringStatus =
            SceneLocalPlayerAdmissionAuthoringStatus.NotValidated;

        [SerializeField, HideInInspector]
        private string lastAuthoringDiagnostic =
            "Scene-Provided Player has not been validated.";

        [NonSerialized]
        private SceneLocalPlayerAdmissionRuntimeHostModule runtimeModule;

        [NonSerialized]
        private SceneLocalPlayerAdmissionRuntimeResult lastRuntimeResult;

        [NonSerialized]
        private string runtimeDiagnostic =
            "Scene-Provided Player runtime is not bound.";

        [NonSerialized]
        private ScenePlayerActorAdoptionResult lastActorAdoptionResult;

        public PlayerSlotProfile PlayerSlotProfile =>
            playerSlotProfile;

        public LocalPlayerHostAuthoring LocalPlayerHost =>
            GetComponent<LocalPlayerHostAuthoring>();

        public ActorProfile ActorProfile =>
            actorProfile;

        public PlayerActorDeclaration SceneLogicalPlayerActor =>
            sceneLogicalPlayerActor;

        public SceneLocalPlayerAdmissionTiming AdmissionTiming =>
            admissionTiming;

        public PlayerActorPhysicalOwnership ActorPhysicalOwnership =>
            PlayerActorPhysicalOwnership.ExternalSceneOwned;

        public SceneLocalPlayerAdmissionAuthoringStatus LastAuthoringStatus =>
            lastAuthoringStatus;

        public string LastAuthoringDiagnostic =>
            lastAuthoringDiagnostic ?? string.Empty;

        public bool RuntimeReady =>
            runtimeModule != null &&
            runtimeModule.IsReadyFor(this);

        public string RuntimeDiagnostic =>
            RuntimeReady
                ? runtimeModule.Diagnostic
                : runtimeDiagnostic ?? string.Empty;

        public SceneLocalPlayerAdmissionRuntimeResult LastRuntimeResult =>
            lastRuntimeResult;

        public ScenePlayerActorAdoptionResult LastActorAdoptionResult =>
            lastActorAdoptionResult;

        public bool HasActiveAdmission =>
            RuntimeReady &&
            runtimeModule.TryGetActiveToken(this, out _);

        public bool HasTypedActorEvidence =>
            evidenceActorProfile != null &&
            evidenceLogicalActorHostPrefab != null;

        public ActorProfile EvidenceActorProfile =>
            evidenceActorProfile;

        public GameObject EvidenceLogicalActorHostPrefab =>
            evidenceLogicalActorHostPrefab;

        public string EvidenceDiagnostic =>
            evidenceDiagnostic ?? string.Empty;

        public bool HasCompleteReferences =>
            playerSlotProfile != null &&
            LocalPlayerHost != null &&
            actorProfile != null &&
            sceneLogicalPlayerActor != null;

        public bool IsTypedActorEvidenceCompatibleWith(
            ActorProfile expectedProfile)
        {
            return expectedProfile != null &&
                ReferenceEquals(
                    evidenceActorProfile,
                    expectedProfile) &&
                expectedProfile.LogicalActorHostPrefab != null &&
                ReferenceEquals(
                    evidenceLogicalActorHostPrefab,
                    expectedProfile.LogicalActorHostPrefab);
        }

        public bool TryGetPlayerSlotId(
            out PlayerSlotId playerSlotId,
            out string issue)
        {
            if (playerSlotProfile == null)
            {
                playerSlotId = default;
                issue =
                    "Scene-Provided Player requires an explicit Player Slot Profile.";
                return false;
            }

            return playerSlotProfile.TryGetPlayerSlotId(
                out playerSlotId,
                out issue);
        }

        public bool TryValidateRuntimeEvidence(
            out string issue)
        {
            LocalPlayerHostAuthoring localPlayerHost =
                LocalPlayerHost;

            if (!HasCompleteReferences)
            {
                issue =
                    "Scene-Provided Player requires Player Slot Profile, same-root Local Player Host, Actor Profile and Scene Logical Player Actor.";
                return false;
            }

            if (!Enum.IsDefined(
                    typeof(SceneLocalPlayerAdmissionTiming),
                    admissionTiming))
            {
                issue =
                    $"Scene-Provided Player has invalid Admission Timing '{admissionTiming}'.";
                return false;
            }

            if (!TryGetPlayerSlotId(out _, out issue))
            {
                return false;
            }

            if (!actorProfile.TryGetActorProfileId(
                    out _,
                    out issue))
            {
                return false;
            }

            if (actorProfile.ActorKind != ActorKind.Player ||
                actorProfile.ActorRole != ActorRole.Protagonist ||
                actorProfile.LogicalActorHostPrefab == null)
            {
                issue =
                    $"Actor Profile '{actorProfile.name}' must define a Player Protagonist Logical Actor Host prefab.";
                return false;
            }

            if (!localPlayerHost.TryValidateAdmissionConfiguration(
                    sceneLogicalPlayerActor,
                    allowExistingLogicalActor: true,
                    out issue))
            {
                return false;
            }

            if (sceneLogicalPlayerActor
                    .GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue =
                    "Scene Logical Player Actor must not contain PlayerInput. PlayerInput belongs to the Local Player Host.";
                return false;
            }

            if (!HasTypedActorEvidence)
            {
                issue =
                    "Scene-Provided Player requires serialized Actor Profile evidence. Run Apply / Rebuild in the Inspector.";
                return false;
            }

            if (!IsTypedActorEvidenceCompatibleWith(actorProfile))
            {
                issue =
                    "Scene-Provided Player evidence does not match the selected Actor Profile and its Logical Actor Host prefab.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        /// <summary>
        /// Explicit manual admission request. Automatic execution is owned by the scoped
        /// lifecycle participant; this component never self-admits from Awake, Start or OnEnable.
        /// </summary>
        public SceneLocalPlayerAdmissionRuntimeResult RequestAdmission(
            RuntimeContentOwner assignmentOwner,
            string source,
            string reason)
        {
            if (!RuntimeReady)
            {
                lastRuntimeResult =
                    SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                        "AdmitSceneLocalPlayer",
                        this,
                        source,
                        reason,
                        RuntimeDiagnostic);
                return lastRuntimeResult;
            }

            return runtimeModule.TryAdmit(
                this,
                assignmentOwner,
                source,
                reason);
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestRelease(
            string source,
            string reason)
        {
            if (!RuntimeReady)
            {
                lastRuntimeResult =
                    SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                        "ReleaseSceneLocalPlayer",
                        this,
                        source,
                        reason,
                        RuntimeDiagnostic);
                return lastRuntimeResult;
            }

            return runtimeModule.TryRelease(
                this,
                source,
                reason);
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestRelease(
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            if (!RuntimeReady)
            {
                lastRuntimeResult =
                    SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                        "ReleaseSceneLocalPlayer",
                        this,
                        source,
                        reason,
                        RuntimeDiagnostic);
                return lastRuntimeResult;
            }

            return runtimeModule.TryRelease(
                this,
                expectedToken,
                source,
                reason);
        }

        internal void BindRuntime(
            SceneLocalPlayerAdmissionRuntimeHostModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(
                    nameof(module));
            }

            if (runtimeModule != null &&
                !ReferenceEquals(runtimeModule, module))
            {
                throw new InvalidOperationException(
                    "Scene-Provided Player is already bound to another Session runtime module.");
            }

            runtimeModule = module;
            runtimeDiagnostic = module.Diagnostic;
        }

        internal void UnbindRuntime(
            SceneLocalPlayerAdmissionRuntimeHostModule module,
            string diagnostic)
        {
            if (runtimeModule != null &&
                ReferenceEquals(runtimeModule, module))
            {
                runtimeModule = null;
            }

            runtimeDiagnostic =
                string.IsNullOrWhiteSpace(diagnostic)
                    ? "Scene-Provided Player runtime is not bound."
                    : diagnostic.Trim();
        }

        internal void SetActorAdoptionResult(
            ScenePlayerActorAdoptionResult result)
        {
            lastActorAdoptionResult = result;
        }

        internal void SetRuntimeResult(
            SceneLocalPlayerAdmissionRuntimeResult result,
            string diagnostic)
        {
            lastRuntimeResult = result;
            runtimeDiagnostic =
                diagnostic ?? string.Empty;
        }

        private void OnDestroy()
        {
            runtimeModule?.HandleAuthoringDestroyed(this);
        }

#if UNITY_EDITOR
        public void EditorSetAuthoringResult(
            SceneLocalPlayerAdmissionAuthoringStatus status,
            string diagnostic)
        {
            lastAuthoringStatus = status;
            lastAuthoringDiagnostic =
                diagnostic ?? string.Empty;
        }

        public void EditorSetProfileEvidence(
            ActorProfile sourceProfile,
            GameObject sourceLogicalActorHostPrefab,
            string diagnostic)
        {
            evidenceActorProfile = sourceProfile;
            evidenceLogicalActorHostPrefab =
                sourceLogicalActorHostPrefab;
            evidenceDiagnostic =
                diagnostic ?? string.Empty;
        }
#endif
    }
}
