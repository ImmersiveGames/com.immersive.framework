using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free declaration of one scene affected by an Activity operation.
    /// This is not a loaded scene handle and it does not execute load or release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation scene entry; planning only.")]
    internal readonly struct ActivityOperationPlanSceneEntry
    {
        public ActivityOperationPlanSceneEntry(
            FrameworkContentIdentity contentIdentity,
            string contentId,
            string sceneName,
            string scenePath,
            FrameworkContentRequiredness requiredness,
            ActivityContentSceneLoadMode loadMode,
            ActivityContentReleasePolicy releasePolicy,
            ActivityOperationSceneAction action,
            int executionOrder,
            bool isAlreadyLoaded)
        {
            ContentIdentity = contentIdentity;
            ContentId = Normalize(contentId);
            SceneName = Normalize(sceneName);
            ScenePath = Normalize(scenePath);
            Requiredness = requiredness;
            LoadMode = loadMode;
            ReleasePolicy = releasePolicy;
            Action = action;
            ExecutionOrder = executionOrder;
            IsAlreadyLoaded = isAlreadyLoaded;
        }

        public FrameworkContentIdentity ContentIdentity { get; }

        public string ContentId { get; }

        public string SceneName { get; }

        public string ScenePath { get; }

        public FrameworkContentScope Scope => FrameworkContentScope.Activity;

        public FrameworkContentKind Kind => FrameworkContentKind.Scene;

        public FrameworkContentRequiredness Requiredness { get; }

        public ActivityContentSceneLoadMode LoadMode { get; }

        public ActivityContentReleasePolicy ReleasePolicy { get; }

        public ActivityOperationSceneAction Action { get; }

        public int ExecutionOrder { get; }

        public bool IsAlreadyLoaded { get; }

        public bool HasContentIdentity => ContentIdentity.IsValid;

        public bool HasScene => !string.IsNullOrWhiteSpace(SceneName) || !string.IsNullOrWhiteSpace(ScenePath);

        public bool IsLoad => Action == ActivityOperationSceneAction.Load;

        public bool IsRelease => Action == ActivityOperationSceneAction.Release;

        public bool IsSceneSideEffect => (IsLoad || IsRelease) && !IsAlreadyLoaded;

        public bool HasBlockingDeclarationIssue => !HasContentIdentity || !HasScene;

        public string ToDiagnosticString()
        {
            string scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            string identity = HasContentIdentity ? ContentIdentity.StableText : "<missing>";
            string contentId = ContentId.ToDiagnosticText("<missing>");
            return $"identity='{identity}' id='{contentId}' action='{Action}' scene='{scene}' requiredness='{Requiredness}' loadMode='{LoadMode}' releasePolicy='{ReleasePolicy}' order='{ExecutionOrder}' alreadyLoaded='{IsAlreadyLoaded}' sideEffect='{IsSceneSideEffect}' blockingDeclarationIssue='{HasBlockingDeclarationIssue}'";
        }

        public static ActivityOperationPlanSceneEntry FromCompositionPlanEntry(
            ActivitySceneCompositionPlanEntry entry,
            bool isAlreadyLoaded)
        {
            return new ActivityOperationPlanSceneEntry(
                entry.ContentIdentity,
                entry.ContentId,
                entry.SceneName,
                entry.ScenePath,
                entry.Requiredness,
                entry.LoadMode,
                entry.ReleasePolicy,
                ActivityOperationSceneAction.Load,
                entry.ExecutionOrder,
                isAlreadyLoaded);
        }

        public static ActivityOperationPlanSceneEntry ForRelease(
            FrameworkContentIdentity contentIdentity,
            string contentId,
            string sceneName,
            string scenePath,
            FrameworkContentRequiredness requiredness,
            ActivityContentSceneLoadMode loadMode,
            ActivityContentReleasePolicy releasePolicy,
            int executionOrder)
        {
            return new ActivityOperationPlanSceneEntry(
                contentIdentity,
                contentId,
                sceneName,
                scenePath,
                requiredness,
                loadMode,
                releasePolicy,
                ActivityOperationSceneAction.Release,
                executionOrder,
                false);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
