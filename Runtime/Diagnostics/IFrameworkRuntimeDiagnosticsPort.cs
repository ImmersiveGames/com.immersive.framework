namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Internal read-only boundary for development diagnostics that need a compact
    /// application runtime snapshot without receiving the concrete runtime owner.
    /// </summary>
    internal interface IFrameworkRuntimeDiagnosticsPort
    {
        FrameworkRuntimeDiagnosticsSnapshot CreateFrameworkRuntimeDiagnosticsSnapshot();
    }
}
