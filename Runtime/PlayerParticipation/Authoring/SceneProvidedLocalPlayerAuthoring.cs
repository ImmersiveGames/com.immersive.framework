using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing Scene-Provided Local Player composition.
    /// The scene supplies an exact Runtime Host and Presentation; ActorProfile supplies only Presentation intent.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Scene-Provided/Local Player")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable Scene-Provided Local Player authoring surface. Manager-Provisioned and Session-Persistent remain Experimental.")]
    public sealed class SceneProvidedLocalPlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private LocalPlayerHostAuthoring localPlayerHost;
        [SerializeField] private PlayerSlotProfile playerSlotProfile;
        [SerializeField] private ActorProfile actorProfile;
        [SerializeField] private PlayerActorRuntimeHost scenePlayerActorRuntimeHost;
        [SerializeField] private GameObject scenePresentation;
        [SerializeField] private SceneLocalPlayerAdmissionTiming admissionTiming = SceneLocalPlayerAdmissionTiming.OnActivityEnter;

        [SerializeField, HideInInspector] private ActorProfile evidenceActorProfile;
        [SerializeField, HideInInspector] private GameObject evidencePresentationPrefab;
        [SerializeField, HideInInspector] private string evidenceDiagnostic = string.Empty;
        [SerializeField, HideInInspector] private SceneProvidedLocalPlayerAuthoringStatus lastAuthoringStatus = SceneProvidedLocalPlayerAuthoringStatus.NotValidated;
        [SerializeField, HideInInspector] private string lastAuthoringDiagnostic = "Scene-Provided Local Player has not been validated.";

        [NonSerialized] private SceneLocalPlayerAdmissionRuntimeHostModule _runtimeModule;
        [NonSerialized] private SceneLocalPlayerAdmissionRuntimeResult _lastRuntimeResult;
        [NonSerialized] private string _runtimeDiagnostic = "Scene-Provided Local Player runtime is not bound.";
        [NonSerialized] private ScenePlayerActorAdoptionResult _lastActorAdoptionResult;

        public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
        public LocalPlayerHostAuthoring LocalPlayerHost => localPlayerHost;
        public ActorProfile ActorProfile => actorProfile;
        public PlayerActorRuntimeHost ScenePlayerActorRuntimeHost => scenePlayerActorRuntimeHost;
        public GameObject ScenePresentation => scenePresentation;
        public PlayerActorDeclaration ScenePlayerActorDeclaration => scenePlayerActorRuntimeHost != null ? scenePlayerActorRuntimeHost.PlayerActorDeclaration : null;
        internal PlayerActorDeclaration SceneLogicalPlayerActor => ScenePlayerActorDeclaration;
        public SceneLocalPlayerAdmissionTiming AdmissionTiming => admissionTiming;
        public SceneProvidedLocalPlayerAuthoringStatus LastAuthoringStatus => lastAuthoringStatus;
        public string LastAuthoringDiagnostic => lastAuthoringDiagnostic ?? string.Empty;
        public bool RuntimeReady => _runtimeModule != null && _runtimeModule.IsReadyFor(this);
        public string RuntimeDiagnostic => RuntimeReady ? _runtimeModule.Diagnostic : _runtimeDiagnostic ?? string.Empty;
        public SceneLocalPlayerAdmissionRuntimeResult LastRuntimeResult => _lastRuntimeResult;
        public ScenePlayerActorAdoptionResult LastActorAdoptionResult => _lastActorAdoptionResult;
        public bool HasActiveAdmission => RuntimeReady && _runtimeModule.TryGetActiveToken(this, out _);
        public PlayerActorPhysicalOwnership ActorPhysicalOwnership => _lastActorAdoptionResult != null && _lastActorAdoptionResult.Succeeded && _lastActorAdoptionResult.Status != ScenePlayerActorAdoptionStatus.SucceededReleased ? PlayerActorPhysicalOwnership.FrameworkOwned : PlayerActorPhysicalOwnership.ExternalSceneOwned;
        public bool HasTypedActorEvidence => evidenceActorProfile != null && evidencePresentationPrefab != null;
        public ActorProfile EvidenceActorProfile => evidenceActorProfile;
        public GameObject EvidencePresentationPrefab => evidencePresentationPrefab;
        public string EvidenceDiagnostic => evidenceDiagnostic ?? string.Empty;
        public bool HasCompleteReferences => playerSlotProfile != null && LocalPlayerHost != null && actorProfile != null && scenePlayerActorRuntimeHost != null && scenePresentation != null;

        public bool IsTypedActorEvidenceCompatibleWith(ActorProfile expectedProfile)
        {
            return expectedProfile != null && evidenceActorProfile == expectedProfile && expectedProfile.PresentationPrefab != null && evidencePresentationPrefab == expectedProfile.PresentationPrefab;
        }

        public bool TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string issue)
        {
            if (playerSlotProfile == null)
            {
                playerSlotId = default;
                issue = "Scene-Provided Local Player requires an explicit Player Slot Profile.";
                return false;
            }
            return playerSlotProfile.TryGetPlayerSlotId(out playerSlotId, out issue);
        }

        public bool TryValidateRuntimeEvidence(out string issue)
        {
            issue = string.Empty;
            LocalPlayerHostAuthoring localPlayerHost = LocalPlayerHost;
            if (!HasCompleteReferences)
            {
                issue = "Scene-Provided Local Player requires explicit Player Slot Profile, Local Player Host, Actor Profile, Player Actor Runtime Host and Presentation references.";
                return false;
            }
            if (!ReferenceEquals(
                    GetComponentInParent<LocalPlayerHostAuthoring>(true),
                    localPlayerHost))
            {
                issue = "Scene-Provided Local Player must reference the nearest ancestral Local Player Host that owns its hierarchy.";
                return false;
            }
            if (!Enum.IsDefined(typeof(SceneLocalPlayerAdmissionTiming), admissionTiming) || !TryGetPlayerSlotId(out _, out issue) || !actorProfile.TryGetActorProfileId(out _, out issue))
            {
                return false;
            }
            if (actorProfile.ActorKind != ActorKind.Player || actorProfile.ActorRole != ActorRole.Protagonist || actorProfile.PresentationPrefab == null)
            {
                issue = $"Actor Profile '{actorProfile.name}' must define a Player Protagonist Presentation prefab.";
                return false;
            }
            if (!localPlayerHost.TryValidateAdmissionConfiguration(scenePlayerActorRuntimeHost, true, out issue)) return false;
            if (scenePresentation.transform.parent != scenePlayerActorRuntimeHost.PresentationMount ||
                scenePresentation.GetComponentInChildren<PlayerInput>(true) != null ||
                scenePresentation.GetComponentInChildren<ActorDeclaration>(true) != null ||
                scenePresentation.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null)
            {
                issue = "Scene Presentation must be the direct child of the exact Presentation Mount and contain no PlayerInput or Framework Actor runtime infrastructure.";
                return false;
            }
            if (!HasTypedActorEvidence || !IsTypedActorEvidenceCompatibleWith(actorProfile))
            {
                issue = "Scene-Provided Local Player evidence does not match the selected Actor Profile Presentation prefab. Run Apply / Rebuild in the Inspector.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestAdmission(RuntimeContentOwner assignmentOwner, string source, string reason)
        {
            if (!RuntimeReady) return _lastRuntimeResult = SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable("AdmitSceneLocalPlayer", this, source, reason, RuntimeDiagnostic);
            return _runtimeModule.TryAdmit(this, assignmentOwner, source, reason);
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestRelease(string source, string reason)
        {
            if (!RuntimeReady) return _lastRuntimeResult = SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable("ReleaseSceneLocalPlayer", this, source, reason, RuntimeDiagnostic);
            return _runtimeModule.TryRelease(this, source, reason);
        }

        public SceneLocalPlayerAdmissionRuntimeResult RequestRelease(SceneLocalPlayerAdmissionToken expectedToken, string source, string reason)
        {
            if (!RuntimeReady) return _lastRuntimeResult = SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable("ReleaseSceneLocalPlayer", this, source, reason, RuntimeDiagnostic);
            return _runtimeModule.TryRelease(this, expectedToken, source, reason);
        }

        internal void BindRuntime(SceneLocalPlayerAdmissionRuntimeHostModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (_runtimeModule != null && !ReferenceEquals(_runtimeModule, module)) throw new InvalidOperationException("Scene-Provided Local Player is already bound to another Session runtime module.");
            _runtimeModule = module;
            _runtimeDiagnostic = module.Diagnostic;
        }

        internal void UnbindRuntime(SceneLocalPlayerAdmissionRuntimeHostModule module, string diagnostic)
        {
            if (_runtimeModule != null && ReferenceEquals(_runtimeModule, module)) _runtimeModule = null;
            _runtimeDiagnostic = string.IsNullOrWhiteSpace(diagnostic) ? "Scene-Provided Local Player runtime is not bound." : diagnostic.Trim();
        }

        internal void SetActorAdoptionResult(ScenePlayerActorAdoptionResult result) => _lastActorAdoptionResult = result;
        internal void SetRuntimeResult(SceneLocalPlayerAdmissionRuntimeResult result, string diagnostic) { _lastRuntimeResult = result; _runtimeDiagnostic = diagnostic ?? string.Empty; }
        private void OnDestroy() => _runtimeModule?.HandleAuthoringDestroyed(this);

#if UNITY_EDITOR
        public void EditorSetAuthoringResult(SceneProvidedLocalPlayerAuthoringStatus status, string diagnostic) { lastAuthoringStatus = status; lastAuthoringDiagnostic = diagnostic ?? string.Empty; }
        public void EditorSetCompositionReferences(PlayerActorRuntimeHost runtimeHost, GameObject presentation)
        {
            scenePlayerActorRuntimeHost = runtimeHost;
            scenePresentation = presentation;
        }
        public void EditorSetProfileEvidence(ActorProfile sourceProfile, GameObject sourcePresentationPrefab, string diagnostic)
        {
            evidenceActorProfile = sourceProfile;
            evidencePresentationPrefab = sourcePresentationPrefab;
            evidenceDiagnostic = diagnostic ?? string.Empty;
        }
#endif
    }
}
