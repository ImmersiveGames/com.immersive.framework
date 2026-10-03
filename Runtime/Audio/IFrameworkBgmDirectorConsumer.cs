namespace Immersive.Framework.Audio
{
    /// <summary>
    /// Internal attachment contract used by the Session-owned BGM authority to bind
    /// Route/Activity consumers for explicit Scene Composition scopes.
    /// </summary>
    internal interface IFrameworkBgmDirectorConsumer
    {
        bool TryAttachBgmDirector(
            FrameworkBgmDirector director,
            out bool wasAlreadyAttached,
            out string issue);

        bool TryDetachBgmDirector(
            FrameworkBgmDirector director,
            out string issue);
    }
}
