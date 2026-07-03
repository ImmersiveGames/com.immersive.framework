using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Immutable runtime identity for content materialized or discovered by the framework.
    /// A handle records ownership and release intent; it does not expose a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public readonly struct FrameworkContentHandle
    {
        public FrameworkContentHandle(
            FrameworkContentIdentity identity,
            FrameworkContentRequiredness requiredness,
            string ownerName,
            string resourceName,
            string resourcePath,
            bool active,
            string source,
            string reason,
            string message)
        {
            if (!identity.IsValid)
            {
                throw new System.ArgumentException("Content identity must be valid.", nameof(identity));
            }

            Identity = identity;
            Scope = identity.Scope;
            Kind = identity.Kind;
            Requiredness = requiredness;
            OwnerId = identity.Owner.Value.Value;
            OwnerName = Normalize(ownerName);
            ContentId = identity.ContentId.StableText;
            ResourceName = Normalize(resourceName);
            ResourcePath = Normalize(resourcePath);
            Active = active;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public FrameworkContentHandle(
            string contentId,
            FrameworkContentScope scope,
            FrameworkContentKind kind,
            FrameworkContentRequiredness requiredness,
            string ownerId,
            string ownerName,
            string resourceName,
            string resourcePath,
            bool active,
            string source,
            string reason,
            string message)
            : this(
                FrameworkContentIdentity.FromOwnerValue(scope, kind, ownerId, contentId),
                requiredness,
                ownerName,
                resourceName,
                resourcePath,
                active,
                source,
                reason,
                message)
        {
        }

        public FrameworkContentIdentity Identity { get; }

        public string ContentId { get; }

        public FrameworkContentScope Scope { get; }

        public FrameworkContentKind Kind { get; }

        public FrameworkContentRequiredness Requiredness { get; }

        public string OwnerId { get; }

        public string OwnerName { get; }

        public string ResourceName { get; }

        public string ResourcePath { get; }

        public bool Active { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasContentId => Identity.IsValid;

        public bool HasResource => !string.IsNullOrWhiteSpace(ResourceName) || !string.IsNullOrWhiteSpace(ResourcePath);

        public FrameworkIdentityKey OwnerIdentity => Identity.Owner;

        public string ToDiagnosticString()
        {
            string resource = !string.IsNullOrWhiteSpace(ResourceName) ? ResourceName : ResourcePath;
            if (string.IsNullOrWhiteSpace(resource))
            {
                resource = "<none>";
            }

            return $"identity='{Identity.StableText}' id='{ContentId}' scope='{Scope}' kind='{Kind}' requiredness='{Requiredness}' owner='{OwnerName}' resource='{resource}' active='{Active}'";
        }

        public static FrameworkContentHandle RoutePrimaryScene(
            string ownerId,
            string ownerName,
            string sceneName,
            string scenePath,
            bool active,
            string source,
            string reason,
            string message)
        {
            string normalizedSceneName = Normalize(sceneName);
            string normalizedScenePath = Normalize(scenePath);
            string sceneIdentity = !string.IsNullOrWhiteSpace(normalizedSceneName)
                ? normalizedSceneName
                : normalizedScenePath;
            if (string.IsNullOrWhiteSpace(sceneIdentity))
            {
                throw new System.ArgumentException("Route primary scene content identity requires a scene name or scene path.", nameof(sceneName));
            }

            string contentId = $"primary-scene:{sceneIdentity}";
            var identity = FrameworkContentIdentity.FromOwnerValue(
                FrameworkContentScope.Route,
                FrameworkContentKind.Scene,
                ownerId,
                contentId);

            return new FrameworkContentHandle(
                identity,
                FrameworkContentRequiredness.Required,
                ownerName,
                sceneName,
                scenePath,
                active,
                source,
                reason,
                message);
        }

        public static FrameworkContentHandle ActivitySceneAuthoredBinding(
            string ownerId,
            string ownerName,
            string objectName,
            string sceneName,
            bool active,
            string source,
            string reason,
            string message)
        {
            string normalizedObjectName = Normalize(objectName);
            string normalizedSceneName = Normalize(sceneName);
            if (string.IsNullOrWhiteSpace(normalizedObjectName))
            {
                throw new System.ArgumentException("Activity scene-authored binding content identity requires an object name.", nameof(objectName));
            }

            string sceneSegment = !string.IsNullOrWhiteSpace(normalizedSceneName)
                ? normalizedSceneName
                : "unknown-scene";
            string contentId = $"scene-authored-binding:{sceneSegment}:{normalizedObjectName}";
            var identity = FrameworkContentIdentity.FromOwnerValue(
                FrameworkContentScope.Activity,
                FrameworkContentKind.SceneAuthored,
                ownerId,
                contentId);

            return new FrameworkContentHandle(
                identity,
                FrameworkContentRequiredness.Optional,
                ownerName,
                objectName,
                sceneSegment,
                active,
                source,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
