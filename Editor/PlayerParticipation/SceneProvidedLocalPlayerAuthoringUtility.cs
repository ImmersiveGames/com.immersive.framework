using System;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    public readonly struct SceneProvidedLocalPlayerAuthoringResult
    {
        public SceneProvidedLocalPlayerAuthoringResult(
            bool succeeded,
            SceneProvidedLocalPlayerAuthoringStatus status,
            string message)
        {
            Succeeded = succeeded;
            Status = status;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public SceneProvidedLocalPlayerAuthoringStatus Status { get; }
        public string Message { get; }
    }

    /// <summary>
    /// Editor-only provenance validation for an already authored Scene-Provided composition.
    /// It never materializes or writes composition state.
    /// </summary>
    public static class SceneProvidedLocalPlayerAuthoringUtility
    {
        public static SceneProvidedLocalPlayerAuthoringResult Validate(
            SceneProvidedLocalPlayerAuthoring authoring,
            bool logDiagnostics = true)
        {
            SceneProvidedLocalPlayerAuthoringResult result = ValidateCore(authoring);
            Record(authoring, result, logDiagnostics);
            return result;
        }

        private static SceneProvidedLocalPlayerAuthoringResult ValidateCore(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            if (!SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out SceneProvidedLocalPlayerComposition composition,
                    out string issue))
            {
                return Failure(SceneProvidedLocalPlayerAuthoringStatus.InvalidHost, issue);
            }

            GameObject runtimeHostSource = ResolveSourcePrefab(
                composition.PlayerActorRuntimeHost.gameObject);
            GameObject expectedRuntimeHost =
                composition.LocalPlayerHost.PlayerActorRuntimeHostPrefab != null
                    ? composition.LocalPlayerHost.PlayerActorRuntimeHostPrefab.gameObject
                    : null;
            if (!AreSamePrefabAsset(runtimeHostSource, expectedRuntimeHost))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidHost,
                    "Scene-Provided Local Player Runtime Host prefab source does not match the Local Player Host Runtime Host prefab.");
            }

            GameObject presentationSource = ResolveSourcePrefab(composition.Presentation);
            if (!AreSamePrefabAsset(
                    presentationSource,
                    authoring.ActorProfile.PresentationPrefab))
            {
                return Failure(
                    SceneProvidedLocalPlayerAuthoringStatus.InvalidActorProfile,
                    "Scene-Provided Local Player Presentation prefab source does not match the selected Actor Profile Presentation prefab.");
            }

            return new SceneProvidedLocalPlayerAuthoringResult(
                true,
                SceneProvidedLocalPlayerAuthoringStatus.Valid,
                "Scene-Provided Local Player authored composition and prefab provenance are valid.");
        }

        private static GameObject ResolveSourcePrefab(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            string assetPath =
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
            return string.IsNullOrWhiteSpace(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private static bool AreSamePrefabAsset(GameObject first, GameObject second)
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
                       out long firstId) &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                       second,
                       out string secondGuid,
                       out long secondId) &&
                   string.Equals(firstGuid, secondGuid, StringComparison.Ordinal) &&
                   firstId == secondId;
        }

        private static SceneProvidedLocalPlayerAuthoringResult Failure(
            SceneProvidedLocalPlayerAuthoringStatus status,
            string message) =>
            new SceneProvidedLocalPlayerAuthoringResult(false, status, message);

        private static void Record(
            SceneProvidedLocalPlayerAuthoring authoring,
            SceneProvidedLocalPlayerAuthoringResult result,
            bool logDiagnostics)
        {
            if (authoring != null)
            {
                authoring.EditorSetAuthoringResult(result.Status, result.Message);
                EditorUtility.SetDirty(authoring);
            }

            if (logDiagnostics)
            {
                FrameworkLogger.Create(typeof(SceneProvidedLocalPlayerAuthoringUtility)).Info(
                    $"[Immersive.Framework][SceneProvidedPlayer] status='{result.Status}' succeeded='{result.Succeeded}' diagnostic='{result.Message}'.");
            }
        }
    }
}
