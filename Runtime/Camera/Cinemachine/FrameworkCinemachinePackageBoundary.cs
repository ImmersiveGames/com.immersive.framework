namespace Immersive.Framework.Camera.Cinemachine
{
    internal static class FrameworkCinemachinePackageBoundary
    {
        internal static string RuntimeAssemblyName => typeof(Unity.Cinemachine.CinemachineBrain).Assembly.GetName().Name;
    }
}
