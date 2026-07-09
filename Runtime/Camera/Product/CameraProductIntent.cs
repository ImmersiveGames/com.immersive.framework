namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Minimal immutable intent contract shared by future CameraRecipe and CameraComposer surfaces.
    /// </summary>
    public readonly struct CameraProductIntent
    {
        public CameraProductIntent(
            CameraMode mode,
            CameraOwnershipScope ownershipScope,
            CameraTargetSourceDescriptor targetSource,
            CameraTargetRequirement followRequirement,
            CameraTargetRequirement lookAtRequirement,
            int priority)
        {
            Mode = mode;
            OwnershipScope = ownershipScope;
            TargetSource = targetSource;
            FollowRequirement = followRequirement;
            LookAtRequirement = lookAtRequirement;
            Priority = priority;
        }

        public CameraMode Mode { get; }

        public CameraOwnershipScope OwnershipScope { get; }

        public CameraTargetSourceDescriptor TargetSource { get; }

        public CameraTargetRequirement FollowRequirement { get; }

        public CameraTargetRequirement LookAtRequirement { get; }

        public int Priority { get; }

        public static CameraProductIntent SinglePlayerFollow(
            CameraTargetSourceDescriptor targetSource,
            int priority,
            CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional)
        {
            return new CameraProductIntent(
                CameraMode.SinglePlayerFollowCamera,
                CameraOwnershipScope.SinglePlayer,
                targetSource,
                CameraTargetRequirement.Required,
                lookAtRequirement,
                priority);
        }
    }
}
