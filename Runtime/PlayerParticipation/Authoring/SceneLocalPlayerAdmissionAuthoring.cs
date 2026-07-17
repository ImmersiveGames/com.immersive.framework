using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing product surface for admitting one local Player Host that already exists
    /// in an Activity scene. It owns authoring intent only; it does not reserve a Slot, assign a
    /// runtime ActorId, enable gameplay or discover runtime authorities by global lookup.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Scene Local Player Admission")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4 designer-first Scene Local Player Admission product surface.")]
    public sealed class SceneLocalPlayerAdmissionAuthoring : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField]
        [Tooltip("Exact configured Player Slot to admit. Runtime never allocates a fallback Slot.")]
        private PlayerSlotProfile playerSlotProfile;

        [SerializeField]
        [Tooltip("Exact technical Local Player Host already present in this Activity scene.")]
        private LocalPlayerHostAuthoring localPlayerHost;

        [Header("Logical Actor")]
        [SerializeField]
        [Tooltip("Exact Actor Profile selected for this scene Player.")]
        private ActorProfile actorProfile;

        [SerializeField]
        [Tooltip("Exact scene Logical Player Actor under Local Player Host / Actor Mount.")]
        private PlayerActorDeclaration sceneLogicalPlayerActor;

        [Header("Admission")]
        [SerializeField]
        private SceneLocalPlayerAdmissionTiming admissionTiming =
            SceneLocalPlayerAdmissionTiming.OnActivityEnter;

        [Header("Debug")]
        [SerializeField, HideInInspector]
        private SceneLocalPlayerAdmissionAuthoringStatus lastAuthoringStatus =
            SceneLocalPlayerAdmissionAuthoringStatus.NotValidated;

        [SerializeField, HideInInspector]
        private string lastAuthoringDiagnostic =
            "Scene Local Player Admission has not been validated.";

        [NonSerialized]
        private SceneLocalPlayerAdmissionRuntimeHostModule runtimeModule;

        [NonSerialized]
        private SceneLocalPlayerAdmissionRuntimeResult lastRuntimeResult;

        [NonSerialized]
        private string runtimeDiagnostic =
            "Scene Local Player Admission runtime is not bound.";

        public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
        public LocalPlayerHostAuthoring LocalPlayerHost => localPlayerHost;
        public ActorProfile ActorProfile => actorProfile;
        public PlayerActorDeclaration SceneLogicalPlayerActor => sceneLogicalPlayerActor;
        public SceneLocalPlayerAdmissionTiming AdmissionTiming => admissionTiming;
        public SceneLocalPlayerAdmissionAuthoringStatus LastAuthoringStatus => lastAuthoringStatus;
        public string LastAuthoringDiagnostic => lastAuthoringDiagnostic ?? string.Empty;
        public bool RuntimeReady => runtimeModule != null && runtimeModule.IsReadyFor(this);
        public string RuntimeDiagnostic => RuntimeReady
            ? runtimeModule.Diagnostic
            : runtimeDiagnostic ?? string.Empty;
        public SceneLocalPlayerAdmissionRuntimeResult LastRuntimeResult => lastRuntimeResult;
        public bool HasActiveAdmission =>
            RuntimeReady && runtimeModule.TryGetActiveToken(this, out _);

        public bool HasCompleteReferences =>
            playerSlotProfile != null &&
            localPlayerHost != null &&
            actorProfile != null &&
            sceneLogicalPlayerActor != null;

        public bool TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string issue)
        {
            if (playerSlotProfile == null)
            {
                playerSlotId = default;
                issue = "Scene Local Player Admission requires an explicit Player Slot Profile.";
                return false;
            }

            return playerSlotProfile.TryGetPlayerSlotId(out playerSlotId, out issue);
        }

        public bool TryValidateRuntimeEvidence(out string issue)
        {
            if (!HasCompleteReferences)
            {
                issue = "Scene Local Player Admission requires Player Slot Profile, Local Player Host, Actor Profile and Scene Logical Player Actor references.";
                return false;
            }

            if (!Enum.IsDefined(typeof(SceneLocalPlayerAdmissionTiming), admissionTiming))
            {
                issue = $"Scene Local Player Admission has invalid Admission Timing '{admissionTiming}'.";
                return false;
            }

            if (!TryGetPlayerSlotId(out _, out issue))
            {
                return false;
            }

            if (!actorProfile.TryGetActorProfileId(out _, out issue))
            {
                return false;
            }

            if (actorProfile.ActorKind != ActorKind.Player ||
                actorProfile.ActorRole != ActorRole.Protagonist ||
                actorProfile.LogicalActorHostPrefab == null)
            {
                issue = $"Actor Profile '{actorProfile.name}' must define a Player Protagonist Logical Actor Host prefab.";
                return false;
            }

            if (!localPlayerHost.TryValidateAdmissionConfiguration(
                    sceneLogicalPlayerActor,
                    allowExistingLogicalActor: true,
                    out issue))
            {
                return false;
            }

            if (sceneLogicalPlayerActor.GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Scene Logical Player Actor must not contain PlayerInput. PlayerInput belongs to the Local Player Host.";
                return false;
            }

            SceneLogicalPlayerActorEvidence evidence =
                sceneLogicalPlayerActor.GetComponent<SceneLogicalPlayerActorEvidence>();
            if (evidence == null)
            {
                issue = "Scene Logical Player Actor requires serialized SceneLogicalPlayerActorEvidence. Run Apply / Rebuild in the Inspector.";
                return false;
            }

            if (!evidence.IsCompatibleWith(actorProfile))
            {
                issue = "Scene Logical Player Actor evidence does not match the selected Actor Profile and its Logical Actor Host prefab.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        /// <summary>
        /// Explicit manual admission request. Automatic On Activity Enter execution is owned by
        /// the scoped Activity lifecycle participant; this component never self-admits from Awake,
        /// Start or OnEnable.
        /// </summary>
        public SceneLocalPlayerAdmissionRuntimeResult RequestAdmission(
            string source,
            string reason)
        {
            if (!RuntimeReady)
            {
                lastRuntimeResult = SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "AdmitSceneLocalPlayer",
                    this,
                    source,
                    reason,
                    RuntimeDiagnostic);
                return lastRuntimeResult;
            }

            return runtimeModule.TryAdmit(this, source, reason);
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestRelease(
            string source,
            string reason)
        {
            if (!RuntimeReady)
            {
                lastRuntimeResult = SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "ReleaseSceneLocalPlayer",
                    this,
                    source,
                    reason,
                    RuntimeDiagnostic);
                return lastRuntimeResult;
            }

            return runtimeModule.TryRelease(this, source, reason);
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestRelease(
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            if (!RuntimeReady)
            {
                lastRuntimeResult = SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "ReleaseSceneLocalPlayer",
                    this,
                    source,
                    reason,
                    RuntimeDiagnostic);
                return lastRuntimeResult;
            }

            return runtimeModule.TryRelease(this, expectedToken, source, reason);
        }

        internal void BindRuntime(SceneLocalPlayerAdmissionRuntimeHostModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (runtimeModule != null && !ReferenceEquals(runtimeModule, module))
            {
                throw new InvalidOperationException(
                    "Scene Local Player Admission is already bound to another Session runtime module.");
            }

            runtimeModule = module;
            runtimeDiagnostic = module.Diagnostic;
        }

        internal void UnbindRuntime(
            SceneLocalPlayerAdmissionRuntimeHostModule module,
            string diagnostic)
        {
            if (runtimeModule != null && ReferenceEquals(runtimeModule, module))
            {
                runtimeModule = null;
            }

            runtimeDiagnostic = string.IsNullOrWhiteSpace(diagnostic)
                ? "Scene Local Player Admission runtime is not bound."
                : diagnostic.Trim();
        }

        internal void SetRuntimeResult(
            SceneLocalPlayerAdmissionRuntimeResult result,
            string diagnostic)
        {
            lastRuntimeResult = result;
            runtimeDiagnostic = diagnostic ?? string.Empty;
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
            lastAuthoringDiagnostic = diagnostic ?? string.Empty;
        }
#endif
    }
}
