using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Serialized Scene-Provided proof that an authored Presentation corresponds to an ActorProfile Presentation prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable Scene-Provided Local Player presentation provenance.")]
    public sealed class ScenePlayerActorPresentationEvidence : MonoBehaviour
    {
        [SerializeField] private ActorProfile actorProfile;
        [SerializeField] private GameObject presentationPrefab;
        [SerializeField, HideInInspector] private string authoringDiagnostic;

        public ActorProfile ActorProfile => actorProfile;
        public GameObject PresentationPrefab => presentationPrefab;
        public string AuthoringDiagnostic => authoringDiagnostic ?? string.Empty;

        public bool IsCompatibleWith(ActorProfile expectedProfile)
        {
            return expectedProfile != null && actorProfile == expectedProfile && expectedProfile.PresentationPrefab != null && presentationPrefab == expectedProfile.PresentationPrefab;
        }

#if UNITY_EDITOR
        public void EditorSetEvidence(ActorProfile sourceProfile, GameObject sourcePresentationPrefab, string diagnostic)
        {
            actorProfile = sourceProfile;
            presentationPrefab = sourcePresentationPrefab;
            authoringDiagnostic = diagnostic ?? string.Empty;
        }
#endif
    }
}
