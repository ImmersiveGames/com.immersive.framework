using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit serialized proof that one scene Logical Player Actor was authored from the
    /// Logical Actor Host prefab selected by an ActorProfile. Runtime code consumes this
    /// evidence without AssetDatabase, PrefabUtility, path, name or tag lookup.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4 serialized source evidence for a scene Logical Player Actor.")]
    public sealed class SceneLogicalPlayerActorEvidence : MonoBehaviour
    {
        [SerializeField] private ActorProfile actorProfile;
        [SerializeField] private GameObject logicalActorHostPrefab;
        [SerializeField, HideInInspector] private string authoringDiagnostic;

        public ActorProfile ActorProfile => actorProfile;
        public GameObject LogicalActorHostPrefab => logicalActorHostPrefab;
        public string AuthoringDiagnostic => authoringDiagnostic ?? string.Empty;

        public bool IsCompatibleWith(ActorProfile expectedProfile)
        {
            return expectedProfile != null &&
                ReferenceEquals(actorProfile, expectedProfile) &&
                expectedProfile.LogicalActorHostPrefab != null &&
                ReferenceEquals(logicalActorHostPrefab, expectedProfile.LogicalActorHostPrefab);
        }

#if UNITY_EDITOR
        public void EditorSetEvidence(
            ActorProfile sourceProfile,
            GameObject sourceLogicalActorHostPrefab,
            string diagnostic)
        {
            actorProfile = sourceProfile;
            logicalActorHostPrefab = sourceLogicalActorHostPrefab;
            authoringDiagnostic = diagnostic ?? string.Empty;
        }
#endif
    }
}
