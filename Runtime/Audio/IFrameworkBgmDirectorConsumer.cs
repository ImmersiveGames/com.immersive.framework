namespace Immersive.Framework.Audio
{
    /// <summary>
    /// Internal attachment contract used by the persistent BGM authority to inject itself into
    /// Route/Activity-scoped BGM bindings as scenes are loaded.
    /// </summary>
    internal interface IFrameworkBgmDirectorConsumer
    {
        void AttachBgmDirector(FrameworkBgmDirector director);

        void DetachBgmDirector(FrameworkBgmDirector director);
    }
}
