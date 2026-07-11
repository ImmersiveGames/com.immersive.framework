namespace Immersive.Framework.Camera
{
    public interface ICameraLifecycleAdapter
    {
        string ScopeId { get; }
        bool IsEntered { get; }

        CameraLifecycleAdapterResult Enter(string scopeId);
        CameraLifecycleAdapterResult Exit(string scopeId);
    }
}
